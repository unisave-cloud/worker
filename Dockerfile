############
# BUILDING #
############

FROM mono:6.12.0 as builder

RUN mkdir -p /watchdog-build
COPY ./ /watchdog-build

RUN msbuild -target:Rebuild -property:Configuration=Release /watchdog-build/Watchdog/Watchdog.csproj
RUN msbuild -target:Rebuild -property:Configuration=Release /watchdog-build/DummyFramework/DummyFramework.csproj
RUN msbuild -target:Rebuild -property:Configuration=Release /watchdog-build/DummyGame/DummyGame.csproj

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

# watchdog folder
RUN mkdir -p /watchdog
COPY --from=builder /watchdog-build/Watchdog/bin/Release /watchdog

# dummy initialization folder
RUN mkdir -p /dummy
COPY --from=builder /watchdog-build/DummyFramework/bin/Release /dummy
COPY --from=builder /watchdog-build/DummyGame/bin/Release /dummy

# game folder
# (part of the tmp folder, since the function will run with read-only filesystem)
RUN mkdir -p /game

# user
RUN groupadd -g 1000 watchdog_group && \
    useradd -u 1000 -m -s /bin/bash -g watchdog_group watchdog_user
RUN chown watchdog_user /game
USER watchdog_user

# workdir
WORKDIR /game

# environment variables
ENV WORKER_HTTP_URL=http://*:8080

# ports
EXPOSE 8080

# entrypoint
ENTRYPOINT ["mono", "--debug", "/watchdog/Watchdog.exe"]
CMD []
