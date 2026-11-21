using ElMomenNursery.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElMomenNursery.Application.Abstractions
{
    public abstract class Observer
    {
        public Subject subject { get; set; }
        abstract public Task<Result<object>> Update(Guid childId);
    }
}
