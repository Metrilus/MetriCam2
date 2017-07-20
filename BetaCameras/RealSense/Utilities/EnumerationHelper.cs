using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetriCam2.Cameras.Utilities
{
    #region Delegates
    delegate pxcmStatus QueryWithDescriptionAndReturnTypeIteratorFunction<D, T>(D descriptionType, int index, out T returnType);
    delegate pxcmStatus QueryWithDescriptionIteratorFunction<T>(T descriptionType, int index, out T returnType);
    delegate pxcmStatus QueryIteratorFunction<T>(int index, out T returnType);
    #endregion

    internal static class EnumerationHelper
    {
        internal static IEnumerable<T> QueryValuesWithDescription<D, T>(D description, QueryWithDescriptionAndReturnTypeIteratorFunction<D, T> queryIterator)
        {
            int i = 0;
            T current;

            while (queryIterator(description, i++, out current) == pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                yield return current;
            }
        }

        internal static IEnumerable<T> QueryValuesWithDescription<T>(T description, QueryWithDescriptionIteratorFunction<T> queryIterator)
        {
            int i = 0;
            T current;

            while (queryIterator(description, i++, out current) == pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                yield return current;
            }
        }

        internal static IEnumerable<T> QueryValues<T>(QueryIteratorFunction<T> queryIterator)
        {
            int i = 0;
            T current;

            while (queryIterator(i++, out current) == pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                yield return current;
            }
        }
    }
}
