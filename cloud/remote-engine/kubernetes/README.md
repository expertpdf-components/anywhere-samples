# ExpertPdf Anywhere with the remote engine on Kubernetes

The engine as a Deployment behind a Service, and the application as a Deployment in the same namespace.
The engine's Service is named `expertpdf-engine`, the library's default endpoint, so the application needs
no endpoint setting. Works on any cluster: AKS, EKS, GKE, or a local one (kind, minikube, Docker Desktop).

```
client --> expertpdf-app --> Service expertpdf-engine:8080 --> engine pods --> the pages they convert
```

## Files

| file | what it is |
|---|---|
| `engine.yaml` | the engine's Deployment (two replicas), its Service and a PodDisruptionBudget; the token comes from the Secret `expertpdf-engine-token` |
| `app.yaml` | the application's Deployment and Service; the token comes from the same Secret |

## Step by step

You need a cluster and `kubectl` pointed at it, Docker, and a registry the cluster can pull from. The
examples use the namespace `pdf`.

**1. Create the namespace and the token.** The engine and the application both read it from this Secret:

```
kubectl create namespace pdf
kubectl -n pdf create secret generic expertpdf-engine-token --from-literal=token="$(openssl rand -hex 32)"
```

**2. Deploy the engine** - from this folder:

```
kubectl -n pdf apply -f engine.yaml
kubectl -n pdf rollout status deployment/expertpdf-engine
```

Each replica requests 2 CPUs and 2 GiB (4 GiB at most) and runs non-root with a read-only root file
system and an in-memory `/tmp`. `/health` is its startup, readiness and liveness probe. Change
`replicas`, `resources` and the `EXPERTPDF_*` settings in `engine.yaml` to size it; the documentation's
Remote Engine topic lists the settings.

**3. Build and push the application image** - from the samples folder (the one that holds `cloud/` and
`docker/`), to your registry:

```
IMAGE=myregistry.example.com/expertpdf-remote-app:1
docker build --platform linux/amd64 -f docker/remote-engine/Dockerfile -t $IMAGE .
docker push $IMAGE
```

Use `--platform linux/arm64` for arm64 nodes. On a local kind cluster, load the image instead of pushing
it: `kind load docker-image $IMAGE`.

**4. Deploy the application** - from this folder:

```
sed "s|IMAGE|$IMAGE|" app.yaml | kubectl -n pdf apply -f -
kubectl -n pdf rollout status deployment/expertpdf-app
```

**5. Convert** - through a port forward, for this test:

```
kubectl -n pdf port-forward service/expertpdf-app 8080:8080 &
curl -o document.pdf "http://localhost:8080/convert?url=https://www.html-to-pdf.net"
curl "http://localhost:8080/text?url=https://www.html-to-pdf.net"
```

To serve the application, put an Ingress (or a `LoadBalancer` Service) in front of `expertpdf-app`. The
engine needs neither: nothing outside the cluster should reach it.

**6. Delete everything** when you are done:

```
kubectl delete namespace pdf
```

## Notes

- **Another namespace**: an application elsewhere in the cluster sets
  `EXPERTPDF_ENGINE_ENDPOINT=http://expertpdf-engine.pdf.svc.cluster.local:8080` and needs the token in a
  Secret of its own namespace.
- **Rotating the token**: put both tokens in the Secret's `token` key, one per line, and restart the
  engine (`kubectl -n pdf rollout restart deployment/expertpdf-engine`), move the applications to the new
  token, then remove the old one.
- **One engine, several applications**: when they are different callers, set `EXPERTPDF_ENGINE_HTTP_CACHE`
  to `clear` in `engine.yaml`, so no conversion is served a response another one fetched.
- **Limit what the engine can reach**: it fetches the pages it converts from the cluster network. When it
  converts html you do not control, a NetworkPolicy that blocks the cluster's internal addresses and the
  cloud metadata address keeps those pages from reaching them.
- **Local files**: the engine cannot see the application's files - a `file://` url is refused. Convert
  the html as a string, or serve it over http.

The documentation's Remote Engine topic has the engine's settings:
https://docs.html-to-pdf.net/anywhere-library/html/Remote-Engine.htm
