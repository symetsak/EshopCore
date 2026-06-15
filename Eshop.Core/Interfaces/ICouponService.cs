using System.Threading.Tasks;

namespace Eshop.Core.Interfaces
{
    public interface ICouponService
    {
        // Ελέγχει αν το κουπόνι ισχύει και επιστρέφει το ποσό της έκπτωσης που αναλογεί
        Task<decimal> CalculateDiscountAsync(string code, decimal currentSubTotal);
    }
}