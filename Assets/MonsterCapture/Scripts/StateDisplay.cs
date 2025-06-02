using TMPro;
using UnityEngine;

public class StateDisplay : MonoBehaviour
{

    [SerializeField] TMP_Text text;
    [SerializeField] IState stateMachine;

    private void Awake()
    {
        stateMachine = GetComponent<IState>();
    }

    void Update()
    {
        if (stateMachine != null)
        {
            text.text = stateMachine.StateDisplay();
        }
        
    }
}
