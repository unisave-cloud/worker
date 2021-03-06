FROM ghcr.io/openfaas/classic-watchdog:0.1.5 as watchdog

FROM mono:6.4.0

# setup watchdog
COPY --from=watchdog /fwatchdog /usr/bin/fwatchdog
RUN chmod +x /usr/bin/fwatchdog

ENV fprocess='/sandbox/execution.sh'
ENV write_debug="false"

# setup sandbox folder
RUN mkdir -p /sandbox

COPY ./bin/Debug /sandbox
COPY ./initialization.sh /sandbox/initialization.sh
COPY ./execution.sh /sandbox/execution.sh
RUN chmod +x /sandbox/initialization.sh
RUN chmod +x /sandbox/execution.sh

# setup game folder and the user
RUN mkdir -p /game

RUN groupadd -g 1000 sandbox_group
RUN useradd -u 1000 -m -s /bin/bash -g sandbox_group sandbox_user
RUN chown sandbox_user /game

WORKDIR /game

USER sandbox_user

# setup the container
EXPOSE 8080
HEALTHCHECK --interval=3s CMD [ -e /tmp/.lock ] || exit 1
ENTRYPOINT ["/sandbox/initialization.sh"]
CMD []
