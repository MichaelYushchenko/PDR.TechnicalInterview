using PDR.PatientBooking.Data;
using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.Validation;
using System;
using System.Linq;

namespace PDR.PatientBooking.Service.BookingServices.Validation
{
    /// <summary>
    /// <inheritdoc cref="IAddBookingRequestValidator"/>
    /// </summary>
    public class AddBookingRequestValidator : IAddBookingRequestValidator
    {
        private readonly PatientBookingContext _context;

        public AddBookingRequestValidator(PatientBookingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public PdrValidationResult ValidateRequest(AddBookingRequest request)
        {
            var result = new PdrValidationResult(true);

            if (DateInThePast(request, ref result))
                return result;

            if (DoctorIsAlreadyBooked(request, ref result))
            {
                return result;
            }

            return result;
        }

        private bool DoctorIsAlreadyBooked(AddBookingRequest request, ref PdrValidationResult result)
        {
            if (_context.Order.Any(p => p.DoctorId == request.DoctorId && !p.IsCancelled
                            &&
                            (
                                p.StartTime <= request.StartTime && request.StartTime < p.EndTime
                                ||
                                p.StartTime < request.EndTime && request.EndTime <= p.EndTime
                                ||
                                request.StartTime <= p.StartTime && p.EndTime <= request.EndTime
                            ))
                )
            {
                result.PassedValidation = false;
                result.Errors.Add("Requested time is busy.");
                return true;
            }

            return false;
        }

        private bool DateInThePast(AddBookingRequest request, ref PdrValidationResult result)
        {
            if (request.StartTime.ToUniversalTime() < DateTime.UtcNow)
            {
                result.PassedValidation = false;
                result.Errors.Add("Invalid appointment start.");
                return true;
            }

            return false;
        }
    }
}
