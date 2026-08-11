namespace ResourceExtraction;

using System;

public class ResourceRequest(string resourceId, ResourceType? type = null) {
    public string ResourceId { get; set; } = resourceId ?? throw new ArgumentNullException(nameof(resourceId));
    public ResourceType? Type { get; set; } = type; // Null means try all types
}