using UnityEngine;
using VContainer.Unity;
using VContainer;

public class GameLifeTimeScope : LifetimeScope {



    [SerializeField] int cellSlotResolution = 2;
    [SerializeField] float padding = 0.9f;
    [SerializeField] float spawnAreaRatio = 0.28f;
    [SerializeField] float gridSpawnGapRatio = 0.04f;
    [SerializeField] float cellSpacingRatio = 0.1f;
    [SerializeField] float cellVisualZOffsetRatio = 0.5f;
    [SerializeField] float jellySizeRatio = 0.82f;
    [SerializeField] float jellyGapRatio = 0.08f;

    protected override void Configure(IContainerBuilder builder) {

        builder.RegisterInstance(Camera.main);
        builder.Register<PlayScreen>(Lifetime.Singleton)
            .AsSelf()
            .WithParameter("camera", Camera.main)
            .WithParameter("column", 6)
            .WithParameter("row", 6)
            .WithParameter("cellSlotResolution", cellSlotResolution)
            .WithParameter("padding", padding)
            .WithParameter("spawnAreaRatio", spawnAreaRatio)
            .WithParameter("gridSpawnGapRatio", gridSpawnGapRatio)
            .WithParameter("cellSpacingRatio", cellSpacingRatio)
            .WithParameter("cellVisualZOffsetRatio", cellVisualZOffsetRatio)
            .WithParameter("jellySizeRatio", jellySizeRatio)
            .WithParameter("jellyGapRatio", jellyGapRatio);

        builder.RegisterComponentInHierarchy<GridManager>();
        builder.RegisterComponentInHierarchy<JellyCubeFactory>();
        builder.RegisterComponentInHierarchy<SpawnManager>();
        builder.RegisterComponentInHierarchy<ManagerGateway>();
        builder.RegisterComponentInHierarchy<Controller>();



    }

}
