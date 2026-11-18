# ExpertPdf Anywhere with the remote engine on Amazon ECS (Fargate)

Two ECS services in one cluster, joined by **ECS Service Connect**: the engine publishes itself as
`expertpdf-engine:8080` - the library's default endpoint, so the application needs no endpoint setting -
and only the application's security group may reach it.

```
client --> expertpdf-app (port 8080) --> Service Connect: expertpdf-engine:8080 --> the pages it converts
```

## Files

| file | what it is |
|---|---|
| `engine-task.json` | the engine's task definition: 2 vCPU / 4 GB, read-only root file system, the token from Secrets Manager |
| `app-task.json` | the application's task definition: 0.5 vCPU / 1 GB, `EXPERTPDF_RENDERING_ENGINE=ChromiumRemote`, the same token |

Both contain the placeholders `ACCOUNT_ID`, `REGION` and `SECRET_ARN`; step 6 fills them in.

## Step by step

You need an AWS account, the [AWS CLI](https://aws.amazon.com/cli/) signed in (`aws configure` or
`aws sso login`) with permissions for ECS, ECR, IAM roles, Secrets Manager, Cloud Map, EC2 security groups
and CloudWatch Logs, and Docker. The steps use the account's default VPC.

**1. Choose the region:**

```
REGION=eu-north-1
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
export AWS_REGION=$REGION
```

**2. Build and push the application image** - from the samples folder (the one that holds
`cloud/` and `docker/`):

```
aws ecr create-repository --repository-name expertpdf-remote-app
aws ecr get-login-password | docker login --username AWS --password-stdin $ACCOUNT_ID.dkr.ecr.$REGION.amazonaws.com
docker build --platform linux/amd64 -f docker/remote-engine/Dockerfile -t $ACCOUNT_ID.dkr.ecr.$REGION.amazonaws.com/expertpdf-remote-app:1 .
docker push $ACCOUNT_ID.dkr.ecr.$REGION.amazonaws.com/expertpdf-remote-app:1
```

**3. Store the token in Secrets Manager:**

```
SECRET_ARN=$(aws secretsmanager create-secret --name expertpdf-engine-token \
  --secret-string "$(openssl rand -hex 32)" --query ARN --output text)
```

**4. Create the task execution role** - it pulls the images, writes the logs and reads the token:

```
aws iam create-role --role-name expertpdfTaskExecutionRole --assume-role-policy-document \
  '{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"Service":"ecs-tasks.amazonaws.com"},"Action":"sts:AssumeRole"}]}'
aws iam attach-role-policy --role-name expertpdfTaskExecutionRole \
  --policy-arn arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy
aws iam put-role-policy --role-name expertpdfTaskExecutionRole --policy-name read-engine-token --policy-document \
  "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Action\":\"secretsmanager:GetSecretValue\",\"Resource\":\"$SECRET_ARN\"}]}"
aws logs create-log-group --log-group-name /ecs/expertpdf-engine
aws logs create-log-group --log-group-name /ecs/expertpdf-app
```

**5. Create the cluster and the security groups.** The cluster's Service Connect namespace is
`expertpdf`; the cluster becomes active once that namespace exists, which takes about a minute. The engine accepts port 8080 from the application's security group only; the application
accepts it from your own address only, for this test:

```
aws ecs create-cluster --cluster-name expertpdf --service-connect-defaults namespace=expertpdf
until [ "$(aws ecs describe-clusters --clusters expertpdf --query 'clusters[0].status' --output text)" = ACTIVE ]; do sleep 5; done
VPC=$(aws ec2 describe-vpcs --filters Name=is-default,Values=true --query 'Vpcs[0].VpcId' --output text)
SUBNETS=$(aws ec2 describe-subnets --filters Name=vpc-id,Values=$VPC Name=default-for-az,Values=true \
  --query 'Subnets[].SubnetId' --output text | tr '\t' ',')
APP_SG=$(aws ec2 create-security-group --group-name expertpdf-app --description "ExpertPdf application" \
  --vpc-id $VPC --query GroupId --output text)
ENGINE_SG=$(aws ec2 create-security-group --group-name expertpdf-engine --description "ExpertPdf engine" \
  --vpc-id $VPC --query GroupId --output text)
aws ec2 authorize-security-group-ingress --group-id $ENGINE_SG --protocol tcp --port 8080 --source-group $APP_SG
aws ec2 authorize-security-group-ingress --group-id $APP_SG --protocol tcp --port 8080 \
  --cidr "$(curl -s https://checkip.amazonaws.com)/32"
```

**6. Register the two task definitions** - from this folder:

```
for f in engine-task.json app-task.json; do
  sed -e "s|ACCOUNT_ID|$ACCOUNT_ID|g" -e "s|REGION|$REGION|g" -e "s|SECRET_ARN|$SECRET_ARN|g" $f > filled-$f
  aws ecs register-task-definition --cli-input-json file://filled-$f --query taskDefinition.taskDefinitionArn
done
```

**7. Start the engine** as a Service Connect server under the name `expertpdf-engine`:

```
aws ecs create-service --cluster expertpdf --service-name expertpdf-engine --task-definition expertpdf-engine \
  --launch-type FARGATE --desired-count 1 \
  --network-configuration "awsvpcConfiguration={subnets=[$SUBNETS],securityGroups=[$ENGINE_SG],assignPublicIp=ENABLED}" \
  --service-connect-configuration '{"enabled":true,"namespace":"expertpdf","services":[{"portName":"engine",
    "discoveryName":"expertpdf-engine","clientAliases":[{"port":8080,"dnsName":"expertpdf-engine"}]}]}'
aws ecs wait services-stable --cluster expertpdf --services expertpdf-engine
```

The engine needs a public IP address (or a NAT gateway in a private subnet) to pull its image from
ghcr.io and to fetch the pages it converts; its security group still lets nothing in but the application.

**8. Start the application** as a Service Connect client:

```
aws ecs create-service --cluster expertpdf --service-name expertpdf-app --task-definition expertpdf-app \
  --launch-type FARGATE --desired-count 1 \
  --network-configuration "awsvpcConfiguration={subnets=[$SUBNETS],securityGroups=[$APP_SG],assignPublicIp=ENABLED}" \
  --service-connect-configuration '{"enabled":true,"namespace":"expertpdf"}'
aws ecs wait services-stable --cluster expertpdf --services expertpdf-app
```

**9. Convert:**

```
TASK=$(aws ecs list-tasks --cluster expertpdf --service-name expertpdf-app --query 'taskArns[0]' --output text)
ENI=$(aws ecs describe-tasks --cluster expertpdf --tasks $TASK \
  --query "tasks[0].attachments[0].details[?name=='networkInterfaceId'].value" --output text)
APP=$(aws ec2 describe-network-interfaces --network-interface-ids $ENI \
  --query 'NetworkInterfaces[0].Association.PublicIp' --output text)
curl -o document.pdf "http://$APP:8080/convert?url=https://www.html-to-pdf.net"
curl "http://$APP:8080/text?url=https://www.html-to-pdf.net"
```

In production put the application behind an Application Load Balancer with HTTPS instead of a public IP.

**10. Delete everything** when you are done:

```
aws ecs delete-service --cluster expertpdf --service expertpdf-app --force
aws ecs delete-service --cluster expertpdf --service expertpdf-engine --force
aws ecs wait services-inactive --cluster expertpdf --services expertpdf-app expertpdf-engine
aws ecs delete-cluster --cluster expertpdf
NS=$(aws servicediscovery list-namespaces --query "Namespaces[?Name=='expertpdf'].Id" --output text)
aws servicediscovery delete-namespace --id $NS
for f in expertpdf-engine expertpdf-app; do
  for td in $(aws ecs list-task-definitions --family-prefix $f --query taskDefinitionArns --output text); do
    aws ecs deregister-task-definition --task-definition $td > /dev/null
    aws ecs delete-task-definitions --task-definitions $td > /dev/null
  done
  aws logs delete-log-group --log-group-name /ecs/$f
done
aws ec2 delete-security-group --group-id $ENGINE_SG
aws ec2 delete-security-group --group-id $APP_SG
aws ecr delete-repository --repository-name expertpdf-remote-app --force
aws secretsmanager delete-secret --secret-id expertpdf-engine-token --force-delete-without-recovery
aws iam delete-role-policy --role-name expertpdfTaskExecutionRole --policy-name read-engine-token
aws iam detach-role-policy --role-name expertpdfTaskExecutionRole \
  --policy-arn arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy
aws iam delete-role --role-name expertpdfTaskExecutionRole
rm -f filled-engine-task.json filled-app-task.json
```

A security group can be deleted only once the tasks' network interfaces are gone; if the command says
it is still in use, wait a minute and repeat it.

## Notes

- **More engines**: raise `--desired-count` of the engine service; Service Connect spreads the
  application's requests over the replicas.
- **One engine, several applications**: any service in the `expertpdf` namespace with Service Connect on
  and the token can use the engine. When they are different callers, add
  `{ "name": "EXPERTPDF_ENGINE_HTTP_CACHE", "value": "clear" }` to the engine's `environment`, so no
  conversion is served a response another one fetched.
- **Local files**: the engine cannot see the application's files - a `file://` url is refused. Convert
  the html as a string, or serve it over http.

The documentation's Remote Engine topic has the engine's settings, and running the engine as a second
container in the application's task instead: https://docs.html-to-pdf.net/anywhere-library/html/Remote-Engine.htm
