using Cysharp.Threading.Tasks;

namespace _Project.Develop.Runtime.Common.SceneManagement
{
    public interface ISceneLoader
    {
        UniTask LoadSceneAsync(string sceneName);
    }
}
