using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        LogAvailableShaders();
        //CreateTestParticles();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void CreateTestParticles()
    {
        GameObject testObj = new GameObject("TestParticles");
        ParticleSystem ps = testObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startSize = 1f;
        main.startSpeed = 1f;
        main.startLifetime = 5f;

        ps.Play();
    }
    void LogAvailableShaders()
    {
        var shaders = Resources.FindObjectsOfTypeAll<Shader>();
        foreach (var shader in shaders)
        {
            Debug.Log(shader.name);
        }
    }
}
