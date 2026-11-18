# ExpertPdf Anywhere on AWS Elastic Beanstalk

The ExpertPdf Anywhere web sample (`../../windows/Web` and `../../linux/Web`) deployed to AWS Elastic
Beanstalk, on either of its two platforms:

- **Windows Server 2022 running IIS 10.0** - the Windows web sample, published self-contained for
  `win-x64` and installed as the IIS site by a deployment manifest;
- **Amazon Linux 2023 running .NET 10** - the Linux web sample (with `ExpertPdf.Anywhere.Fonts`),
  started by a `Procfile` behind the platform's nginx.

Nothing else is needed on the instances: no packages, no fonts, no `chmod`, no configuration files.

## What is in this folder

| file | |
|---|---|
| `build-bundles.ps1` / `build-bundles.sh` | publish both web samples and build the two source bundles in `out/` |
| `windows/aws-windows-deployment-manifest.json` | tells the Windows platform to install `site.zip` as the root site of IIS |
| `linux/Procfile` | the command the Linux platform runs: `dotnet ExpertPdf.Anywhere.Samples.dll` on port 5000 |

The Windows bundle is published **self-contained**, so it does not depend on the .NET versions the
platform installs. The Linux bundle is framework-dependent and needs the ".NET 10" platform.

## Step by step

You need an AWS account and either the console or the AWS CLI (`aws configure`). The examples use the
`eu-north-1` region.

**1. Build the bundles** - from this folder:

```
powershell -ExecutionPolicy Bypass -File build-bundles.ps1
```

(or `sh build-bundles.sh` on Linux and macOS). This writes `out/expertpdf-eb-windows.zip` and
`out/expertpdf-eb-linux.zip`.

**2. Create the environment in the console** (Elastic Beanstalk > Create application):

1. Platform: **.NET on Windows Server** (Windows Server 2022, IIS 10.0) or **.NET 10 running on 64bit
   Amazon Linux 2023**.
2. Application code: **Upload your code**, and pick the matching zip from `out/`.
3. Presets: **Single instance** for a test, **High availability** for production.
4. Service access: let the console create the service role and the EC2 instance profile
   (`aws-elasticbeanstalk-service-role`, `aws-elasticbeanstalk-ec2-role`) if you have none.
5. Instance type: at least **2 vCPU and 8 GB** (for example `m7i-flex.large`, `m7i.large` or
   `t3.large`). Chromium is not usable on a 1 GB instance.
6. Create the environment and wait until its health is **Ok**; then open its domain
   (`<env>.<region>.elasticbeanstalk.com`) - the sample's home page lists every conversion.

**Or with the AWS CLI**, once the two roles exist (the console creates them the first time):

```
aws elasticbeanstalk create-storage-location --region eu-north-1
aws s3 cp out/expertpdf-eb-linux.zip s3://elasticbeanstalk-eu-north-1-123456789012/expertpdf/expertpdf-eb-linux.zip
aws elasticbeanstalk create-application --application-name expertpdf --region eu-north-1
aws elasticbeanstalk create-application-version --application-name expertpdf --version-label linux-1 --source-bundle S3Bucket=elasticbeanstalk-eu-north-1-123456789012,S3Key=expertpdf/expertpdf-eb-linux.zip --region eu-north-1
aws elasticbeanstalk create-environment --application-name expertpdf --environment-name expertpdf-linux --version-label linux-1 --solution-stack-name "64bit Amazon Linux 2023 v3.11.9 running .NET 10" --option-settings Namespace=aws:elasticbeanstalk:environment,OptionName=EnvironmentType,Value=SingleInstance Namespace=aws:autoscaling:launchconfiguration,OptionName=InstanceType,Value=m7i-flex.large Namespace=aws:autoscaling:launchconfiguration,OptionName=IamInstanceProfile,Value=aws-elasticbeanstalk-ec2-role Namespace=aws:elasticbeanstalk:environment,OptionName=ServiceRole,Value=aws-elasticbeanstalk-service-role --region eu-north-1
```

For Windows, upload `expertpdf-eb-windows.zip` and use the solution stack
`64bit Windows Server 2022 v2.23.6 running IIS 10.0`. `aws elasticbeanstalk list-available-solution-stacks`
lists the current versions; replace `123456789012` with your account number.

## What to expect

On a single `m7i-flex.large` instance (2 vCPU / 8 GB):

- live web sites convert on both platforms (English, Arabic, Japanese, Chinese and Korean pages
  included);
- the instance keeps the Chromium engine loaded between requests; the first conversion after a
  deployment also starts the engine;
- the self-contained .NET 10 Windows site needs nothing from the platform beyond IIS;
- new accounts on the AWS free plan can only start the instance types the plan allows - `t3.large`
  is refused there, `m7i-flex.large` is accepted.

## Removing it

Terminate the environment (Actions > Terminate environment) and delete the application; the S3 bucket
Elastic Beanstalk created (`elasticbeanstalk-<region>-<account>`) keeps the uploaded bundles until you
empty and delete it.

See the AWS Elastic Beanstalk topic of the documentation for details:
https://docs.html-to-pdf.net/anywhere-library/html/AWS-Elastic-Beanstalk.htm
