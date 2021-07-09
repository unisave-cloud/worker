FROM ghcr.io/openfaas/of-watchdog:0.8.4 as watchdog

# TODO: perform compilation here as well

FROM mono:6.4.0

# setup watchdog
COPY --from=watchdog /fwatchdog /usr/bin/fwatchdog
RUN chmod +x /usr/bin/fwatchdog

# sandbox folder
RUN mkdir -p /sandbox
COPY ./bin/Debug /sandbox

# game folder
RUN mkdir -p /game

# user
RUN groupadd -g 1000 sandbox_group && \
    useradd -u 1000 -m -s /bin/bash -g sandbox_group sandbox_user
RUN chown sandbox_user /game
USER sandbox_user

# workdir
WORKDIR /game

# environment variables
ENV fprocess="mono --debug /sandbox/UnisaveSandbox.exe"
ENV mode="http"
ENV upstream_url="http://localhost:5000"
ENV max_inflight=1
ENV write_debug="false"

ENV SANDBOX_SERVER_PORT=5000

# ports
EXPOSE 8080
EXPOSE 8081
EXPOSE 5000

# healthcheck
HEALTHCHECK --interval=3s CMD [ -e /tmp/.lock ] || exit 1

# entrypoint
ENTRYPOINT ["fwatchdog"]
CMD []
