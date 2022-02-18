VERSION=$$(grep -oP "AssemblyInformationalVersion\(\"\K[^\"]+" Watchdog/Properties/AssemblyInfo.cs)
TAG=registry.digitalocean.com/unisave/unisave-watchdog:$(VERSION)

.PHONY: build push run run-sh

build:
	docker build --tag $(TAG) .

push:
	docker push $(TAG)

run:
	docker run --rm -it -p 8080:8080 \
	-e WATCHDOG_DUMMY_INITIALIZATION=true \
	-e REQUEST_TIMEOUT_SECONDS=3 \
	$(TAG)

run-sh:
	docker run --rm -it --entrypoint bash $(TAG)
