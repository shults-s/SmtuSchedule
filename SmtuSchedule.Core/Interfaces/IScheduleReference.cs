using System;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core.Interfaces
{
    public interface IScheduleReference
    {
        Int32 ScheduleId { get; set; }

        String Name { get; set; }

        void Validate()
        {
            if (String.IsNullOrWhiteSpace(Name))
            {
                throw new ValidationException("Property 'Name' must be set.");
            }
        }
    }
}