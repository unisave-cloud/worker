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
