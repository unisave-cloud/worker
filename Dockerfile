############
# BUILDING #
############

FROM mono:6.12.0 AS builder

RUN mkdir -p /worker-build
COPY ./ /worker-build

RUN msbuild -target:Rebuild -property:Configuration=Release /worker-build/UnisaveWorker/UnisaveWorker.csproj


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

# worker folder
RUN mkdir -p /worker
COPY --from=builder /worker-build/UnisaveWorker/bin/Release /worker

# game folder
RUN mkdir -p /game

# user
RUN groupadd -g 1000 worker_group && \
    useradd -u 1000 -m -s /bin/bash -g worker_group worker_user
RUN chown worker_user /game
USER worker_user

# workdir
WORKDIR /game

# environment variables
ENV WORKER_HTTP_URL=http://*:8080

# ports
EXPOSE 8080

# entrypoint
ENTRYPOINT ["mono", "--debug", "/worker/UnisaveWorker.exe"]
CMD []
