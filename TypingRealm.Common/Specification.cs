﻿using System;
using System.Linq.Expressions;

namespace TypingRealm;

public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();

        return predicate(entity);
    }
}
