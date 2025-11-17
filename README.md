# Athena Client Library

This is a dotnet library for interacting with the Athena API (Resolver Unknown CSAM Detection).

Documentation can be found [here](https://crispthinking.github.io/athena-dotnet-client/).

## Runnable Examples

You can find runnable examples in the `examples` directory of this repository.

Many examples make use of `.env` files to manage configuration. You can create
a `.env` file in the root of the repository to set environment variables for
the examples. Files beginning `.env` are configured to be ignored by git, to
avoid accidentally committing sensitive information. Each example will describe
any required configuration or command line arguments.

### Resolver.Athena.CliClient

This example is a simple dotnet console application that can:
- Test OAuth credentials to ensure token acquisition is working
- Can perform a synchronous classification of a single image.

To get started, run the following dotnet command:
```bash
dotnet run --project examples/Resolver.Athena.CliClient -- --help
```

## Development

This package is built with .NET 9.0. You can use the .NET CLI to build and test the library.

### Basic Commands

To build the project, run:

```bash
dotnet build
```

To run tests, use:

```bash
dotnet test
```

To format the code, run:
```bash
dotnet format
```

### Pre-Commit Hooks

This project uses `pre-commit` to manage pre-commit hooks.

Installation documentation can be found [here](https://pre-commit.com/index.html#install).

To install the pre-commit hooks, run:

```bash
pre-commit install
```

These hooks will run automatically before each commit to ensure code quality and consistency.

If you want to run them manually, you can use:

```bash
pre-commit run
```

### Building the documentation

First, ensure you have DocFX installed. You can install it via `dotnet`:

```bash
dotnet tool install -g docfx
```

You will also need to install `DocLinkChecker` if you want to validate links in
the documentation. You can install it via `dotnet`:

```bash
dotnet tool install -g DocLinkChecker
```

The documentation is built using DocFX. To build and locally serve the
documentation, run:

```bash
docfx docs/docfx.json --serve
```

This will build the documentation and serve it at `http://localhost:8080`.

To ensure that all of the links in the documentation are valid, you can run:

```bash
DocLinkChecker -d docs
```
