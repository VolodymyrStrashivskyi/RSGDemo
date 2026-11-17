using _Project.Develop.Runtime.Common.SceneManagement;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Infrastructure
{
    public class AppEntryPoint : MonoBehaviour
    {
        private ISceneLoader _sceneLoader;

        [Inject]
        public void Construct(ISceneLoader sceneLoader) =>
            _sceneLoader = sceneLoader;
        
        public void Awake() => 
            Application.targetFrameRate = 144;

        public async void Start() => 
            await _sceneLoader.LoadSceneAsync(SceneList.Gameplay);
    }
}
