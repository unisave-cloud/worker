# TODO: perform compilation here as well

FROM mono:6.4.0

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
ENV SANDBOX_SERVER_PORT=8080

# ports
EXPOSE 8080

# entrypoint
ENTRYPOINT ["mono", "--debug", "/sandbox/UnisaveSandbox.exe"]
CMD []
