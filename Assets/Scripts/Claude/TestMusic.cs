using RhythmGen;
using UnityEngine;

public class TestMusic : MonoBehaviour
{
    public ChartBuilder chartBuilder;
    public ChartGraphicReactor chartGraphicReactor;

    void Start()
    {
        chartBuilder.Build();
        chartGraphicReactor.Activate();
        chartBuilder.GetComponent<AudioSource>().clip = chartBuilder.song;
        chartBuilder.GetComponent<AudioSource>().Play();
    }
}
