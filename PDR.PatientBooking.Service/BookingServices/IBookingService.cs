using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.BookingServices.Responses;

namespace PDR.PatientBooking.Service.BookingServices
{
    public interface IBookingService
    {
        void AddBooking(AddBookingRequest request);
        void CancelBooking(CancelBookingRequest request);
        GetPatientNextAppointmentResponse GetPatientNextAppointment(long identificationNumber);
    }
}
