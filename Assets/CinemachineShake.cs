using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CinemachineShake : MonoBehaviour
{
    public static CinemachineShake Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private CinemachineVirtualCamera _cam;
    private CinemachineBasicMultiChannelPerlin m_channelsPerlin;
    private float shakerTimer = 0;

    // Start is called before the first frame update
    void Start()
    {
        _cam = GetComponent<CinemachineVirtualCamera>();
        m_channelsPerlin = _cam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    public void Shake(float intensity, float time)
    {
        m_channelsPerlin.m_AmplitudeGain = intensity;
        shakerTimer = time;
    }

    private void Update()
    {
        if(shakerTimer > 0) {
            shakerTimer -= Time.deltaTime;
            if(shakerTimer <= 0f)
            {
                m_channelsPerlin.m_AmplitudeGain = 0f;
            }
        }
    }
}
