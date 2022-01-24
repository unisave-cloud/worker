############
# BUILDING #
############

FROM mono:6.12.0 as builder

RUN mkdir -p /sandbox-build
COPY ./ /sandbox-build

RUN msbuild -target:Rebuild -property:Configuration=Release /sandbox-build/UnisaveSandbox/UnisaveSandbox.csproj
RUN msbuild -target:Rebuild -property:Configuration=Release /sandbox-build/DummyFramework/DummyFramework.csproj
RUN msbuild -target:Rebuild -property:Configuration=Release /sandbox-build/DummyGame/DummyGame.csproj

###########
# RUNNING #
###########

FROM mono:6.12.0

# remove the DST_Root_X3.pem certificate as it confuses Mono HTTP client
# (it's already expired but some let's encrypt certs still reference it)
# (including unisave certificates)
#
# Use the cert-sync tool:
# https://www.mono-project.com/docs/faq/security/
RUN ls /etc/ssl/certs/ \
        | grep \\.pem\$ \
        | grep --invert-match DST_Root_CA_X3\\.pem \
        | xargs -I{} cat /etc/ssl/certs/{} \
        > /root/certs-tmp.crt \
    && cert-sync /root/certs-tmp.crt \
    && rm /root/certs-tmp.crt

# sandbox folder
RUN mkdir -p /sandbox
COPY --from=builder /sandbox-build/UnisaveSandbox/bin/Release /sandbox

# dummy initialization folder
RUN mkdir -p /dummy
COPY --from=builder /sandbox-build/DummyFramework/bin/Release /dummy
COPY --from=builder /sandbox-build/DummyGame/bin/Release /dummy

# game folder
# (part of the tmp folder, since the function will run with read-only filesystem)
RUN mkdir -p /game

# user
RUN groupadd -g 1000 sandbox_group && \
    useradd -u 1000 -m -s /bin/bash -g sandbox_group sandbox_user
RUN chown sandbox_user /game
USER sandbox_user

# workdir
WORKDIR /game

# environment variables
ENV SANDBOX_SERVER_PORT=8080
ENV SANDBOX_DUMMY_INITIALIZATION=false
ENV REQUEST_TIMEOUT_SECONDS=30

# ports
EXPOSE 8080

# entrypoint
ENTRYPOINT ["mono", "--debug", "/sandbox/UnisaveSandbox.exe"]
CMD []
