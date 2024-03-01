// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;

namespace Aspire.Hosting;

/// <summary>
/// An Aspire resource that supports use of Azure Provisioning APIs to create Azure resources.
/// </summary>
/// <param name="name"></param>
/// <param name="configureConstruct"></param>
public class AzureConstructResource(string name, Action<ResourceModuleConstruct> configureConstruct) : AzureBicepResource(name, templateFile: $"{name}.module.bicep")
{
    /// <summary>
    /// Callback for configuring construct.
    /// </summary>
    public Action<ResourceModuleConstruct> ConfigureConstruct { get; } = configureConstruct;

    /// <inheritdoc/>
    public override BicepTemplateFile GetBicepTemplateFile(string? directory = null, bool deleteTemporaryFileOnDispose = true)
    {
        var configuration = new Configuration()
        {
            UsePromptMode = true
        };

        var resourceModuleConstruct = new ResourceModuleConstruct(this, configuration);

        foreach (var aspireParameter in this.Parameters)
        {
            var constructParameter = new Parameter(aspireParameter.Key);
            resourceModuleConstruct.AddParameter(constructParameter);
        }

        ConfigureConstruct(resourceModuleConstruct);

        var generationPath = Directory.CreateTempSubdirectory("aspire").FullName;
        resourceModuleConstruct.Build(generationPath);

        var moduleSourcePath = Path.Combine(generationPath, "main.bicep");
        var moduleDestinationPath = Path.Combine(directory ?? generationPath, $"{Name}.module.bicep");
        File.Copy(moduleSourcePath, moduleDestinationPath!, true);

        return new BicepTemplateFile(moduleDestinationPath, directory is null);
    }
}

/// <summary>
/// Extensions for working with <see cref="AzureConstructResource"/> and related types.
/// </summary>
public static class AzureConstructResourceExtensions
{
    /// <summary>
    /// Adds an Azure construct resource to the application model.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource being added.</param>
    /// <param name="configureConstruct">A callback used to configure the construct resource.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureConstructResource> AddAzureConstruct(this IDistributedApplicationBuilder builder, string name, Action<ResourceModuleConstruct> configureConstruct)
    {
        var resource = new AzureConstructResource(name, configureConstruct);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds a parameter to the Azure construct resource based on an Aspire parameter.
    /// </summary>
    /// <param name="resourceModuleConstruct">The Azure construct resource.</param>
    /// <param name="parameterResourceBuilder">The Aspire parameter resource builder.</param>
    /// <returns></returns>
    public static Parameter AddParameter(this ResourceModuleConstruct resourceModuleConstruct, IResourceBuilder<ParameterResource> parameterResourceBuilder)
    {
        return resourceModuleConstruct.AddParameter(parameterResourceBuilder.Resource.Name, parameterResourceBuilder);
    }

    /// <summary>
    /// Adds a parameter to the Azure construct resource based on an Aspire parameter.
    /// </summary>
    /// <param name="resourceModuleConstruct">The Azure construct resource.</param>
    /// <param name="name">The name to be used for the Azure construct parameter.</param>
    /// <param name="parameterResourceBuilder">The Aspire parameter resource builder.</param>
    /// <returns></returns>
    public static Parameter AddParameter(this ResourceModuleConstruct resourceModuleConstruct, string name, IResourceBuilder<ParameterResource> parameterResourceBuilder)
    {
        // Ensure the parameter is added to the Aspire resource.
        resourceModuleConstruct.Resource.Parameters.Add(name, parameterResourceBuilder);

        var parameter = new Parameter(name, isSecure: parameterResourceBuilder.Resource.Secret);
        resourceModuleConstruct.AddParameter(parameter);
        return parameter;
    }

    /// <summary>
    /// Adds a parameter to the Azure construct resource based on an <see cref="BicepOutputReference"/>
    /// </summary>
    /// <param name="resourceModuleConstruct">The Azure construct resource.</param>
    /// <param name="outputReference">The Aspire Bicep output reference.</param>
    /// <returns></returns>
    public static Parameter AddParameter(this ResourceModuleConstruct resourceModuleConstruct, BicepOutputReference outputReference)
    {
        return resourceModuleConstruct.AddParameter(outputReference.Name, outputReference);
    }

    /// <summary>
    /// Adds a parameter to the Azure construct resource based on an <see cref="BicepOutputReference"/>
    /// </summary>
    /// <param name="resourceModuleConstruct">The Azure construct resource.</param>
    /// <param name="name">The name to be used for the Azure construct parameter.</param>
    /// <param name="outputReference">The Aspire Bicep output reference.</param>
    /// <returns></returns>
    public static Parameter AddParameter(this ResourceModuleConstruct resourceModuleConstruct, string name, BicepOutputReference outputReference)
    {
        resourceModuleConstruct.Resource.Parameters.Add(name, outputReference);

        var parameter = new Parameter(name);
        resourceModuleConstruct.AddParameter(parameter);
        return parameter;
    }
}

/// <summary>
/// An Azure Provisioning construct which represents the root Bicep module that is generated for an Azure construct resource.
/// </summary>
public class ResourceModuleConstruct : Infrastructure
{
    internal ResourceModuleConstruct(AzureConstructResource resource, Configuration configuration) : base(constructScope: ConstructScope.ResourceGroup, tenantId: Guid.Empty, subscriptionId: Guid.Empty, envName: "temp", configuration: configuration)
    {
        Resource = resource;
    }

    /// <summary>
    /// The Azure cosntruct resource that this resource module construct represents.
    /// </summary>
    public AzureConstructResource Resource { get; }
}
