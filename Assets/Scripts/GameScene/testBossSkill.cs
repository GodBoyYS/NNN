using Unity.Netcode;
using UnityEngine;

public class testBossSkill : NetworkBehaviour
{
    public SkillDataSO[] skills;
    public LayerMask goundLayer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if( Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, 1000f, goundLayer))
            {
                var targetpos = hit.point;
                transform.position = targetpos;
            }
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("boss释放技能2");
            skills[0].Cast(gameObject, null, transform.position);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            Debug.Log("boss释放大招");
            skills[1].Cast(gameObject, null, transform.position);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("boss释放镭射");
            skills[2].Cast(gameObject, null, transform.position);
        }
    }
}
