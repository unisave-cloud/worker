VERSION=$$(grep -oP "AssemblyInformationalVersion\(\"\K[^\"]+" Properties/AssemblyInfo.cs)
TAG=registry.digitalocean.com/unisave/unisave-sandbox:$(VERSION)

.PHONY: build

build:
	docker build --tag $(TAG) .

push:
	docker push $(TAG)
