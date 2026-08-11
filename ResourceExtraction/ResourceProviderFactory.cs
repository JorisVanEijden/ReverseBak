namespace ResourceExtraction {
    using ResourceExtraction.Extractors;
    using ResourceExtraction.Extractors.Audio;
    using ResourceExtraction.Providers;

    public static class ResourceProviderFactory {
        public static IResourceProvider CreateResourceProvider(string gameDirectory) {
            var compositeProvider = new CompositeResourceProvider();

            // Add archive provider for general resources
            compositeProvider.AddProvider(new GeneralResourceProvider(gameDirectory));

            // Add audio provider for audio resources
            compositeProvider.AddProvider(new AudioResourceProvider(gameDirectory));

            return compositeProvider;
        }
    }
}