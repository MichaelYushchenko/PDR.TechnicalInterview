using PDR.PatientBooking.Data;
using PDR.PatientBooking.Data.Models;
using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.BookingServices.Responses;
using PDR.PatientBooking.Service.BookingServices.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDR.PatientBooking.Service.BookingServices
{
    public class BookingService : IBookingService
    {
        private readonly PatientBookingContext _context;
        private readonly IAddBookingRequestValidator _addBookingRequestValidator;

        public BookingService(PatientBookingContext context, IAddBookingRequestValidator addBookingRequestValidator)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _addBookingRequestValidator = addBookingRequestValidator ?? throw new ArgumentNullException(nameof(addBookingRequestValidator));
        }

        public void AddBooking(AddBookingRequest request)
        {
            var validationResult = _addBookingRequestValidator.ValidateRequest(request);

            if (!validationResult.PassedValidation)
            {
                throw new ArgumentException(validationResult.Errors.First());
            }

            var bookingId = new Guid();
            var bookingStartTime = request.StartTime;
            var bookingEndTime = request.EndTime;
            var bookingPatientId = request.PatientId;
            var bookingPatient = _context.Patient.FirstOrDefault(x => x.Id == request.PatientId);
            var bookingDoctorId = request.DoctorId;
            var bookingDoctor = _context.Doctor.FirstOrDefault(x => x.Id == request.DoctorId);
            var bookingSurgeryType = _context.Patient.FirstOrDefault(x => x.Id == bookingPatientId).Clinic.SurgeryType;

            var myBooking = new Order
            {
                Id = bookingId,
                StartTime = bookingStartTime,
                EndTime = bookingEndTime,
                PatientId = bookingPatientId,
                DoctorId = bookingDoctorId,
                Patient = bookingPatient,
                Doctor = bookingDoctor,
                IsCancelled = false,
                SurgeryType = (int)bookingSurgeryType
            };

            _context.Order.AddRange(new List<Order> { myBooking });
            _context.SaveChanges();
        }

        public GetPatientNextAppointmentResponse GetPatientNextAppointment(long identificationNumber)
        {
            var booking = _context.Order
                                  .Where(p => p.PatientId == identificationNumber && !p.IsCancelled && p.StartTime > DateTime.Now)
                                  .OrderBy(p => p.StartTime)
                                  .First();
            
            return new GetPatientNextAppointmentResponse
            {
                Id = booking.Id,
                DoctorId = booking.DoctorId,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime
            };
        }

        public void CancelBooking(CancelBookingRequest request)
        {
            var order = _context.Order
                                .First(p => p.Id == request.Id);

            order.IsCancelled = true;

            _context.Update(order);
            _context.SaveChanges();
        }
    }
}
