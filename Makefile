VERSION=$$(grep -oP "AssemblyInformationalVersion\(\"\K[^\"]+" UnisaveWorker/Properties/AssemblyInfo.cs)
TAG=registry.digitalocean.com/unisave/unisave-worker:$(VERSION)
MONO_TAG=$(TAG)-mono

.PHONY: build push run run-sh
.PHONY: build-mono push-mono run-mono run-sh-mono

build:
	docker build -f Dockerfile.dotnet --tag $(TAG) .

build-mono:
	docker build -f Dockerfile.mono --tag $(MONO_TAG) .

push:
	docker push $(TAG)

push-mono:
	docker push $(MONO_TAG)

run:
	docker run --rm -it \
		--net host \
		--memory 250m \
		--cpus 0.25 \
	$(TAG)

run-mono:
	docker run --rm -it \
		--net host \
		--memory 250m \
		--cpus 0.25 \
	$(MONO_TAG)

run-sh:
	@docker run --rm -it --net host --entrypoint bash $(TAG)

run-sh-mono:
	@docker run --rm -it --net host --entrypoint bash $(MONO_TAG)
