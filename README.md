# fiaphackaton
Dois micro serviços para Hackaton Esperança Solidária

### Como Rodar o projeto Localmente
#### Habilitar o Kubernetes
Docker Desktop → Settings → Kubernetes → marque "Enable Kubernetes" → Apply & Restart. Confirme o contexto:

kubectl config use-context docker-desktop
kubectl get nodes

#### Instalar Ingress NGINX e metrics-server (necessários para os Ingress e os HPAs)

kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/cloud/deploy.yaml
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

kubectl wait --namespace ingress-nginx --for=condition=ready pod --selector=app.kubernetes.io/component=controller --timeout=120s

#### Buildar as três imagens

cd ~/Desktop/hachathon
docker build -t campaignuserservice-api:local -f CampaignUserService/Dockerfile CampaignUserService
docker build -t donationservice-api:local -f DonationService/docker/Dockerfile.Api DonationService
docker build -t donationservice-worker:local -f DonationService/docker/Dockerfile.Worker DonationService

####  Aplicar toda a infraestrutura
kubectl apply -k k8s/
kubectl get pods -n campaign-system -w

#### Acessar RabbitMQ / Postgres / Mongo (via port-forward, credenciais nos respectivos Secrets)
kubectl port-forward -n campaign-system svc/rabbitmq 15672:15672   # http://localhost:15672
kubectl port-forward -n campaign-system svc/postgres 5432:5432
kubectl port-forward -n campaign-system svc/mongodb 27017:27017


