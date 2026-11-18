# ExpertPdf Anywhere on AWS Lambda

An AWS Lambda function that converts a web page or an HTML string to PDF and returns it over HTTP
(a Lambda function URL, or an API Gateway HTTP API).

```
GET  https://<function-url>/?url=https://www.html-to-pdf.net    converts a web page
POST https://<function-url>/   (body: html)                      converts an HTML string
```

The function is deployed as a **container image**. A zip deployment is not possible: the Chromium
engine and its libraries are larger than the 250 MB a zip-based Lambda function can hold.

## What is in the image

- the function (`csharp/Function.cs`), built for .NET 10 on the `public.ecr.aws/lambda/dotnet:10` base image;
- `ExpertPdf.Anywhere.Linux` (or `ExpertPdf.Anywhere.Linux.Arm64` for an arm64 function) - the library with
  the Chromium engine and every library it needs;
- `ExpertPdf.Anywhere.Fonts` - fonts for every language. The Lambda image has no fonts installed;
  without this package only Latin text renders, and Arabic, Chinese, Japanese, Korean, Hindi, Thai and
  other scripts print as empty boxes;
- `csharp/fonts/` - your own fonts, if your HTML uses fonts by name (see the README in that folder).

Nothing is installed in the image and no `chmod` is needed: the function runs as Lambda's sandbox
user, and the engine package makes the engine files executable for any user when the image is built.

## Step by step

You need an AWS account, the AWS CLI (`aws configure` with an access key of a user allowed to use
Lambda, ECR and IAM), and Docker. The examples use the `eu-north-1` region and account `123456789012`;
replace both.

**1. Build the image** - from the samples folder (the one that holds `cloud/`):

```
docker build --platform linux/amd64 --provenance=false --sbom=false -f cloud/aws-lambda/Dockerfile -t expertpdf-lambda .
```

`--provenance=false --sbom=false` matter: Lambda refuses the multi-manifest images Docker builds by
default ("The image manifest, config or layer media type ... is not supported").

**2. Push it to Amazon ECR:**

```
aws ecr create-repository --repository-name expertpdf-lambda --region eu-north-1
aws ecr get-login-password --region eu-north-1 | docker login --username AWS --password-stdin 123456789012.dkr.ecr.eu-north-1.amazonaws.com
docker tag expertpdf-lambda 123456789012.dkr.ecr.eu-north-1.amazonaws.com/expertpdf-lambda:latest
docker push 123456789012.dkr.ecr.eu-north-1.amazonaws.com/expertpdf-lambda:latest
```

**3. Create an execution role** (in the console: IAM > Roles > Create role > AWS service > Lambda,
with the `AWSLambdaBasicExecutionRole` policy), or:

```
aws iam create-role --role-name expertpdf-lambda-role --assume-role-policy-document "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":{\"Service\":\"lambda.amazonaws.com\"},\"Action\":\"sts:AssumeRole\"}]}"
aws iam attach-role-policy --role-name expertpdf-lambda-role --policy-arn arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole
```

**4. Create the function** - 2048 MB of memory (Lambda gives CPU in proportion to memory) and a
120 second timeout:

```
aws lambda create-function --function-name expertpdf-lambda --package-type Image --code ImageUri=123456789012.dkr.ecr.eu-north-1.amazonaws.com/expertpdf-lambda:latest --role arn:aws:iam::123456789012:role/expertpdf-lambda-role --memory-size 2048 --timeout 120 --region eu-north-1
```

**5. Give it a URL.** With `--auth-type AWS_IAM` callers must sign their requests; `NONE` makes the
URL public, so use it only for a quick test:

```
aws lambda create-function-url-config --function-name expertpdf-lambda --auth-type AWS_IAM --region eu-north-1
```

**6. Call it.** Without a URL, the CLI can invoke the function directly:

```
aws lambda invoke --function-name expertpdf-lambda --cli-binary-format raw-in-base64-out --payload "{\"version\":\"2.0\",\"rawPath\":\"/\",\"queryStringParameters\":{\"url\":\"https://www.html-to-pdf.net\"},\"requestContext\":{\"http\":{\"method\":\"GET\"}}}" response.json --region eu-north-1
```

The PDF is base64 in the `body` of `response.json`.

### arm64 (Graviton)

Build with `--platform linux/arm64 --build-arg RID=linux-arm64` and create the function with
`--architectures arm64`. arm64 Lambda is cheaper per second and converts at about the same speed.

### PDFs larger than 4.4 MB

A Lambda response is limited to 6 MB, and the PDF is base64-encoded in it, so about 4.4 MB of PDF fits.
For larger documents, create an S3 bucket, set the function's `PDF_BUCKET` environment variable to its
name, and allow the role to write to it (`s3:PutObject` and `s3:GetObject` on `arn:aws:s3:::<bucket>/pdf/*`).
The function then stores such a PDF in the bucket and redirects to a temporary (15 minute) link.
For such a PDF the function answers `302` with the presigned link, the link downloads the PDF, and
the object itself is not readable without the signature.

### License key

Without a license key the library runs in evaluation mode and marks the documents it produces. Set
`converter.LicenseKey` in `Function.cs`, or `LicensingManager.LicenseKey` once at start-up; keep the key
in an environment variable or AWS Secrets Manager rather than in the image.

## What to expect

With 2048 MB of memory:

- live web sites convert on x86_64 and arm64;
- the first conversion in a new Lambda environment also starts the engine (a cold start); provisioned
  concurrency keeps environments warm;
- for heavy pages (large news and marketing sites), raise the function timeout.

## Testing locally

The base image contains the Lambda runtime interface emulator:

```
docker run --rm -p 9000:8080 expertpdf-lambda
curl -X POST http://localhost:9000/2015-03-31/functions/function/invocations -d "{\"version\":\"2.0\",\"rawPath\":\"/\",\"queryStringParameters\":{\"url\":\"https://www.html-to-pdf.net\"},\"requestContext\":{\"http\":{\"method\":\"GET\"}}}"
```

See the AWS Lambda topic of the documentation for details:
https://docs.html-to-pdf.net/anywhere-library/html/AWS-Lambda.htm
