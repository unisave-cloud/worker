VERSION=$$(grep -oP "AssemblyInformationalVersion\(\"\K[^\"]+" Properties/AssemblyInfo.cs)
TAG=registry.digitalocean.com/unisave/unisave-sandbox:$(VERSION)

.PHONY: build push deploy run

build:
	docker build --tag $(TAG) .

push:
	docker push $(TAG)
	
deploy:
	bash deploy.sh

run:
	docker run --rm -it -p 8080:8080 -p 8081:8081 -p 5000:5000 -e write_debug=true $(TAG)
