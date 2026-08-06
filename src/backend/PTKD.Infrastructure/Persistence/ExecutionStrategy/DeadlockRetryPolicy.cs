using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;

namespace PTKD.Infrastructure.Persistence.Retries;

public class DeadlockRetryPolicy : Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy
{
    private const int DeadlockErrorNumber = 1205;

    public DeadlockRetryPolicy(ExecutionStrategyDependencies dependencies, int maxRetryCount, TimeSpan maxRetryDelay) 
        : base(dependencies, maxRetryCount, maxRetryDelay)
    {
    }

    protected override bool ShouldRetryOn(Exception exception)
    {
        if (exception is SqlException sqlException)
        {
            foreach (SqlError error in sqlException.Errors)
            {
                if (error.Number == DeadlockErrorNumber)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
