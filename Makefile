VERSION=$$(grep -oP "AssemblyInformationalVersion\(\"\K[^\"]+" UnisaveWorker/Properties/AssemblyInfo.cs)
TAG=registry.digitalocean.com/unisave/unisave-worker:$(VERSION)

.PHONY: build push run run-sh

build:
	docker build --tag $(TAG) .

push:
	docker push $(TAG)

run:
	docker run --rm -it -p 8080:8080 \
		--memory 250m \
		--cpus 0.25 \
	$(TAG)

run-sh:
	@docker run --rm -it --entrypoint bash $(TAG)
