using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;

namespace Signapse
{
    /// <summary>
    /// Allows returning multiple endpoint convention builders from middleware to apply the same policies to each of them
    /// </summary>
    public class EndpointConventionCombiner : List<IEndpointConventionBuilder>, IEndpointConventionBuilder
    {
        public void Add(Action<EndpointBuilder> convention)
        {
            foreach (var ep in this)
            {
                ep.Add(convention);
            }
        }
    }
}