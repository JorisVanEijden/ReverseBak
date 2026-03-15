namespace ResourceExtraction {
    using GameData.Resources;
    using System;
    using System.Collections.Generic;
    using System.IO;

    public interface IResourceProvider {
        ResourceType ResourceType { get; }
        bool CanProvideResource(string resourceId);
        Stream GetResourceStream(string resourceId);
        IDictionary<string, (long, uint)> GetDictionary(ResourceType? type = null);

        /// <summary>
        /// Gets a resource of the specified type directly
        /// </summary>
        /// <typeparam name="T">The type of resource to retrieve, must implement IResource</typeparam>
        /// <param name="resourceId">The ID of the resource to retrieve</param>
        /// <returns>The requested resource</returns>
        /// <exception cref="InvalidOperationException">Thrown when the provider cannot provide the requested resource type</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the resource with the specified ID is not found</exception>
        T GetResource<T>(string resourceId) where T : IResource;
    }
}