namespace GameData.Resources;

public interface IHaveSubResource<out T> where T : IResource {
    public T GetSubResource(int index);
}