using UnityEngine;

[DefaultExecutionOrder(1000)]
public class TutorialSceneBootstrap : MonoBehaviour
{
    [Tooltip("필수 참조를 이름/계층 탐색으로 보완하지 않습니다. 모두 Inspector에서 직접 연결합니다.")]
    [SerializeField] private TutorialStageFacade _stageFacade;
    [SerializeField] private TutorialActorDirector _actorDirector;
    [SerializeField] private TutorialTransientRegistry _transientRegistry;
    [SerializeField] private TutorialDirector _director;

    private void Start()
    {
        if (_stageFacade == null
            || _actorDirector == null
            || _transientRegistry == null
            || _director == null)
        {
            Debug.LogError("TutorialSceneBootstrap 필수 참조가 누락됐습니다.", this);
            enabled = false;
            return;
        }

        _transientRegistry.ActivateForTutorialScene();
        if (!_stageFacade.InitializeFacade())
        {
            enabled = false;
            return;
        }
        if (!ApplyTutorialSkybox())
        {
            enabled = false;
            return;
        }

        // DB 전체 학생의 명시적 동기 초기화가 전부 반환된 뒤에만 Director를 시작한다.
        if (!_actorDirector.InitializePool())
        {
            enabled = false;
            return;
        }

        if (!_director.InitializeDirector())
            enabled = false;
    }



    private bool ApplyTutorialSkybox()
    {
        StageRuntimeConfig runtimeConfig = _stageFacade.RuntimeConfig;
        if (runtimeConfig == null || !runtimeConfig.IsTutorial)
        {
            Debug.LogError("튜토리얼 Skybox를 적용할 StageRuntimeConfig가 없습니다.", this);
            return false;
        }
        if (runtimeConfig.TutorialSkybox == null)
        {
            Debug.LogError("Tutorial StageRuntimeConfig의 Tutorial Skybox를 Inspector에서 연결해야 합니다.", runtimeConfig);
            return false;
        }

        RenderSettings.skybox = runtimeConfig.TutorialSkybox;
        DynamicGI.UpdateEnvironment();
        return true;
    }
}
