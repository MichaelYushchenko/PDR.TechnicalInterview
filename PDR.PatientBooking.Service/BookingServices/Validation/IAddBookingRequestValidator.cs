using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.Validation;

namespace PDR.PatientBooking.Service.BookingServices.Validation
{
    /// <summary>
    /// Defines a class that provides validation for adding new booking
    /// </summary>
    public interface IAddBookingRequestValidator
    {
        /// <summary>
        /// Validation method
        /// </summary>
        /// <param name="request">Request to validate</param>
        /// <returns>Validation result <see cref="PdrValidationResult"/></returns>
        PdrValidationResult ValidateRequest(AddBookingRequest request);

    }
}
