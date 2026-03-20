namespace GameCore.Utils.DesignPattern.ObjectPooling
{
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
}