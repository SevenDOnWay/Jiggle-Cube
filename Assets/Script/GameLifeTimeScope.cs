using UnityEngine;
using System.Collections;
using VContainer.Unity;
using VContainer;

public class GameLifeTimeScope : LifetimeScope {

    protected override void Configure(IContainerBuilder builder) {
        
        builder.Register<PlayScreen>(Lifetime.Singleton);


    }

}
