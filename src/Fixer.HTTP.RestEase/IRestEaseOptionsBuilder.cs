using System;
using System.Collections.Generic;
using System.Text;

namespace Fixer.HTTP.RestEase
{
    public interface IRestEaseOptionsBuilder
    {
        IRestEaseOptionsBuilder WithLoadBalancer(string loadBalancer);
        IRestEaseOptionsBuilder WithService(Func<IRestEaseServiceBuilder, IRestEaseServiceBuilder> buildService);
        RestEaseOptions Build();
    }
}
