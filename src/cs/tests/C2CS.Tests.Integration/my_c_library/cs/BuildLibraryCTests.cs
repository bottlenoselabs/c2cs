// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Xunit;

namespace C2CS.IntegrationTests.my_c_library;

[Trait("Integration", "my_c_library")]
public class BuildLibraryCTests
{
    // [Fact]
    // public void Build()
    // {
    //     var buildProject = new BuildProjectCMake();
    //
    //     var request = new Input(buildProject);
    //     var useCase = new UseCase();
    //     var response = useCase.Execute(request);
    //
    //     Assert.True(response.Status == UseCaseOutputStatus.Success);
    //     Assert.True(response.Diagnostics.Length == 0);
    //     Assert.True(!response.BuildTargetResults.IsDefaultOrEmpty);
    // }
}
