# Fib.Net

## What is Fib?
A full C# rewrite of [Google's Jib][jib],
Fib.Net builds optimized [Docker][Docker] and [OCI][OCI] images from .NET
projects without the need for a Dockerfile or docker daemon.

## Usage

To start using Fib.Net to build images for your ASP.NET Core project, 
install the Fib.Net.MSBuild package.

```shell
dotnet install Fib.Net.MSBuild
```

Then just run a publish targeting the FibDotNet publish provider.

```shell
dotnet publish -p:PublishProvider=FibDotNet
```

This will default to publishing the resulting image to your local docker daemon.
The repository name will be the same as your project, and will be tagged with your project version.


## Configuration

Fib.Net.MSBuild uses standard MSBuild Properties and Items to control its build.
These can be set at the command line, or saved in either your project file,
 or an MSBuild [publish profile][PublishProfile].

### Properties

#### General Properties

##### FibPublishType
The type of publish action being performed. Defaults to `Daemon` when not explicitly set.
Valid values are:

- `Daemon` (default): Publishes the image to the local docker daemon using the `docker` command.
- `Push`: Pushes the image to the remote registry.
- `Tar`: Build a tar file of your image.

##### FibOutputTarFile
The path/name of the tar file to be written for publish type `Tar`.
Defaults to `$(OutputPath)$(PackageId).tar`

##### FibImageFormat
The format of the image. Can be [Docker][Docker image format] or [OCI][OCI image format].
Defaults to `Docker`.

##### FibReproducableBuild
When true, sets the time metadata of the image to be Jan 1, 1970.
This allows every build to have the same hash.

#### Base Image Properties

##### FibBaseImage
The base image the final image is built from. By default it is built from `$(FibBaseRegistry)`,
`$(FibBaseRepository)`, `$(FibBaseTag)` and `$(FibBaseDigest)`.

##### FibBaseRegistry
The registry of the base image. Defaults to `mcr.microsoft.com`.

##### FibBaseRepository
The base image repository. Defaults to `dotnet/core/aspnet`.

##### FibBaseTag
The base image tag. Defaults to `$(BundledNETCoreAppTargetFrameworkVersion)`.
Overridden by `$(FibBaseDigest)`.

##### FibBaseDigest
The hash/digest of the base image. Overrides FibBaseImage.

#### Target Image Properties

##### FibTargetImage
The name/full tag of the image being built.
If not set, built from `$(FibTargetRegistry)` and `$(FibTargetRepository)`.

##### FibTargetRegistry
The registry of the image being built. Defaults to '', which means the docker registry.

##### FibTargetRepository
The repository of the image being built. Defaults to [`$(PackageId)`][Pack Target].

##### FibTargetTag
Semicolon separated list of tags to tag your image with.
Gets converted to FibTargetTag items.
Defaults to [`$(PackageVersion)`][Pack Target].

#### Image configuration properties

##### FibEntrypoint
The image [entrypoint][Docker Entrypoint]. Defaults to `dotnet`.

##### FibCmd
The image [cmd][Docker Cmd]. Defaults to the output assembly in the image.

##### FibImageWorkingDirectory
The image [working directory][Docker WorkDir]. 

##### FibImageUser
The image [user][Docker user].

### Items

#### FibImageFile
The list of files to be added to the image.
Fib.Net.MSBuild will take all the files in your publish directory
and add them to the image in various layers.

FibImageFile items require two metadata properties:

- Layer: The layer the file is being added to.
  Fib.Net.MSBuild splits item types into separate layers to improve repeated build speed.
- TargetPath: The location in the image to put the file.

#### FibEnvironment
The [environment variables][Docker env] to add to the image. They have the format `<key>=<value>`.

#### FibPort
The [ports][Docker expose] the image will expose by default.
Format can be any of port number (`80`),
port/protocol (`8080/tcp`) or port range/protocol (`1000-3000/tcp`).

#### FibVolume
The [volume][Docker volume] mount points of the image.

#### FibLabel
The [metadata labels][Docker label] to apply to the image. They have the format `<key>=<value>`.

[jib]: https://github.com/GoogleContainerTools/jib
[OCI]: https://github.com/opencontainers/image-spec
[Docker]: https://www.docker.com/
[PublishProfile]: https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/visual-studio-publish-profiles?view=aspnetcore-3.1#publish-profiles
[Docker Entrypoint]: https://docs.docker.com/engine/reference/builder/#entrypoint
[Docker Cmd]: https://docs.docker.com/engine/reference/builder/#cmd
[Docker WorkDir]: https://docs.docker.com/engine/reference/builder/#workdir
[Docker user]: https://docs.docker.com/engine/reference/builder/#user
[Docker env]: https://docs.docker.com/engine/reference/builder/#env
[Docker expose]: https://docs.docker.com/engine/reference/builder/#expose
[Docker volume]: https://docs.docker.com/engine/reference/builder/#volume
[Docker label]: https://docs.docker.com/engine/reference/builder/#label
[Docker image format]: https://docs.docker.com/registry/spec/manifest-v2-2/
[OCI image format]: https://github.com/opencontainers/image-spec/blob/master/manifest.md
[Pack Target]: https://docs.microsoft.com/en-us/nuget/reference/msbuild-targets#pack-target