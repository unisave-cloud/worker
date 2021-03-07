#!/bin/bash

# simple function deployment script to test things out

REGISTRY_AUTH=$(kubectl get secret/do-registry-key -o jsonpath={.data.\\.dockerconfigjson})

curl -X POST --user root:password http://openfaas.unisave.local/system/functions -d '{
	"service": "sandbox",
	"image": "registry.digitalocean.com/unisave/unisave-sandbox:0.5.0-dev",
	"labels": {},
	"annotations": {},
	"envVars": {
		"INITIALIZATION_RECIPE_URL": "http://web.unisave-dev.svc.cluster.local:8000/_api/sandbox-recipe/123/pEKzqMtILv0CirCCYytGDoZyLCgYvLwi/39ea2c46ca1cbd03a463a9cd586c39d41903f45a4b608ef406fdf57254ea6914"
	},
	"secrets": ["do-registry-key"],
	"limits": {
		"memory": "70M"
    },
    "registryAuth": "'$REGISTRY_AUTH'"
}'
