namespace ResourceExtraction.Providers {
    using GameData.Resources;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using ResourceType = ResourceExtraction.ResourceType;

    public class CompositeResourceProvider : IResourceProvider {
        private readonly List<IResourceProvider> _providers = new();
        private readonly Dictionary<ResourceType, IResourceProvider> _providerByType = new();

        public ResourceType ResourceType {
            get => throw new NotSupportedException("Composite provider doesn't have a single resource type");
        }

        public void AddProvider(IResourceProvider provider) {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            _providers.Add(provider);
            _providerByType[provider.ResourceType] = provider;
        }

        public bool CanProvideResource(string resourceId) {
            return _providers.Any(p => p.CanProvideResource(resourceId));
        }

        public Stream GetResourceStream(ResourceRequest request) {
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (request.Type.HasValue) {
                if (_providerByType.TryGetValue(request.Type.Value, out var provider) && provider.CanProvideResource(request.ResourceId)) {
                    return provider.GetResourceStream(request.ResourceId);
                }

                throw new KeyNotFoundException($"{request.Type} resource {request.ResourceId} not found.");
            }

            // Try specific providers in order of priority
            foreach (var provider in _providers) {
                if (provider.CanProvideResource(request.ResourceId)) {
                    return provider.GetResourceStream(request.ResourceId);
                }
            }

            throw new KeyNotFoundException($"Resource {request.ResourceId} not found in any provider.");
        }

        public Stream GetResourceStream(string resourceId) {
            return GetResourceStream(new ResourceRequest(resourceId));
        }

        public IDictionary<string, (long, uint)> GetDictionary(ResourceType? type = null) {
            if (type.HasValue) {
                return _providerByType.TryGetValue(type.Value, out var provider) ? provider.GetDictionary() : new Dictionary<string, (long, uint)>();
            }

            var result = new Dictionary<string, (long, uint)>();
            foreach (var provider in _providers) {
                foreach (KeyValuePair<string, (long, uint)> entry in provider.GetDictionary()) {
                    result.Add(entry.Key, entry.Value);
                }
            }

            return result;
        }

        public T GetResource<T>(string resourceId) where T : IResource {
            foreach (var provider in _providers) {
                if (provider.CanProvideResource(resourceId)) {
                    try {
                        return provider.GetResource<T>(resourceId);
                    } catch (InvalidOperationException) {
                        // Skip providers that can't provide the requested type
                    }
                }
            }

            throw new KeyNotFoundException($"Resource {resourceId} not found in any provider.");
        }
    }
}