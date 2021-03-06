# Unisave Sandbox

This repository contains the OpenFaas function image, that is used to
run game backends.

The behaviour is as follows:

1. The Unisave system asks OpenFaas to create a new function
   corresponding to a certain backend ID (say `backend_foobar` function)
2. OpenFaas creates new instance of the function and during the startup
   process the function downloads proper backend files
3. The function is now ready to receive execution requests

The initialization is performed by executing the sandbox in the `init`
mode with a signed URL from which an initialization recipe is downloaded
and executed:

    $ sandbox.exe init http://web:8000/_api/sandbox-recipe/...

The execution is then invoked by the watchdog process by calling:

    $ sandbox.exe exec

The executing uses standard input/output as any usual faas function.
