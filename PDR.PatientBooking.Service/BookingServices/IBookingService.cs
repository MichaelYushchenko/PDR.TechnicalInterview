using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.BookingServices.Responses;

namespace PDR.PatientBooking.Service.BookingServices
{
    /// <summary>
    /// Defines a class that provides functionality to work with appointments 
    /// </summary>
    public interface IBookingService
    {
        /// <summary>
        /// Creates a new appointment
        /// </summary>
        /// <param name="request"></param>
        void AddBooking(AddBookingRequest request);

        /// <summary>
        /// Cancels existing appointment
        /// </summary>
        /// <param name="request"></param>
        void CancelBooking(CancelBookingRequest request);

        /// <summary>
        /// Retrives next appointment for a patient
        /// </summary>
        /// <param name="identificationNumber">Patient identifier</param>
        /// <returns>Appointment information</returns>
        GetPatientNextAppointmentResponse GetPatientNextAppointment(long identificationNumber);
    }
}
