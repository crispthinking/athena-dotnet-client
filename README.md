# Athena Client Library

This is a dotnet library for interacting with the Athena API (Resolver Unknown CSAM Detection).

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
