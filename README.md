# AzureDevOpsEndOfSprintBranchCutter

The `AzureDevOpsEndOfSprintBranchCutter` is a tool designed to automate the process of cutting and tagging release branches based on the development branch for repositories within an Azure DevOps Organization Account. This tool simplifies the end-of-sprint workflow by creating release branches and associated tags in a consistent and efficient manner.

## Features

- **Automated Release Branching:** Streamline the process of cutting release branches from the development branch at the end of each sprint.

- **Tagging for Releases:** Automatically create tags for the newly cut release branches, allowing for easy tracking and versioning.

- **Flexible Configuration:** Customize the tool's behavior through the AppSettings configuration. Specify the repositories, branch names, and tag formats to match your organization's workflow.

## Getting Started

Follow these steps to get started with the `AzureDevOpsEndOfSprintBranchCutter`:

1. Generate a Personal Access Token (PAT) for your Azure DevOps account. This token will be used for authentication and access to the Azure DevOps API.

2. Clone this repository to your local machine.

3. Open the solution in your preferred IDE or code editor.

4. Replace all instances of "PlaceHolder" with the name of your Azure DevOps Organization Account in the codebase.

5. In the `AppSettings.json` file, configure the repositories for which you want to run the Sprint Cutter. Provide the repository names, source and destination branch names, and tag formats.

6. Build the solution to ensure everything compiles correctly.

7. Run the application. The tool will automatically cut release branches and create tags based on your configuration.

## Configuration

In the `AppSettings.json` file, configure the following settings:

- `AzureDevOpsPAT`: Your Azure DevOps Personal Access Token.

- `AzureDevOpsBaseUrl`: The base URL for your Azure DevOps organization.

- `Repositories`: An array of repository configurations containing:
  - `RepositoryName`: The name of the repository.
  - `SourceBranch`: The name of the development branch to cut releases from.
  - `DestinationBranchPrefix`: Prefix for the release branches.
  - `TagFormat`: Format for creating tags for each release branch.
---

**Note:** To use this tool, you will need a valid Azure DevOps account, a Personal Access Token (PAT), and proper configuration of repositories, branches, and tags in the `AppSettings.json` file.
