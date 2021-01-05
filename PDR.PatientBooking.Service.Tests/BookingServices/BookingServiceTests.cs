using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using PDR.PatientBooking.Data;
using PDR.PatientBooking.Data.Models;
using PDR.PatientBooking.Service.BookingServices;
using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.BookingServices.Responses;
using PDR.PatientBooking.Service.BookingServices.Validation;
using PDR.PatientBooking.Service.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDR.PatientBooking.Service.Tests.BookingServices
{
    public class BookingServiceTests
    {
        private MockRepository _mockRepository;
        private IFixture _fixture;

        private PatientBookingContext _context;
        private Mock<IAddBookingRequestValidator> _validator;

        private BookingService _bookingService;

        private (DateTime Start, DateTime End) appointmentBaseTime => (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, 12, 0, 0),
                                                                       new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, 12, 15, 0));

        [SetUp]
        public void SetUp()
        {
            // Boilerplate
            _mockRepository = new MockRepository(MockBehavior.Strict);
            _fixture = new Fixture();

            //Prevent fixture from generating circular references
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            // Mock setup
            _context = new PatientBookingContext(new DbContextOptionsBuilder<PatientBookingContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            _validator = _mockRepository.Create<IAddBookingRequestValidator>();

            // Mock default
            SetupMockDefaults();

            // Sut instantiation
            _bookingService = new BookingService(
                _context,
                _validator.Object
            );
        }

        private void SetupMockDefaults()
        {
            _validator.Setup(x => x.ValidateRequest(It.IsAny<AddBookingRequest>()))
                .Returns(new PdrValidationResult(true));
        }

        [Test]
        public void AddBooking_ValidateRequest()
        {
            //arrange
            var clinic = _fixture.Create<Clinic>();
            var doctor = _fixture.Create<Doctor>();
            var patient = _fixture.Create<Patient>();

            patient.ClinicId = clinic.Id;

            _context.Clinic.Add(clinic);
            _context.Doctor.Add(doctor);
            _context.Patient.Add(patient);

            _context.SaveChanges();

            var request = _fixture.Create<AddBookingRequest>();
            request.PatientId = patient.Id;
            request.DoctorId = doctor.Id;

            //act
            _bookingService.AddBooking(request);

            //assert
            _validator.Verify(x => x.ValidateRequest(request), Times.Once);
        }

        [Test]
        public void AddBooking_ValidatorFails_ThrowsArgumentException()
        {
            //arrange
            var failedValidationResult = new PdrValidationResult(false, _fixture.Create<string>());

            _validator.Setup(x => x.ValidateRequest(It.IsAny<AddBookingRequest>())).Returns(failedValidationResult);

            //act
            var exception = Assert.Throws<ArgumentException>(() => _bookingService.AddBooking(_fixture.Create<AddBookingRequest>()));

            //assert
            exception.Message.Should().Be(failedValidationResult.Errors.First());
        }

        [Test]
        public void AddBooking_AddsOrderToContextWithGeneratedId()
        {
            //arrange
            var order = _fixture.Create<Order>();
            order.IsCancelled = false;
            order.StartTime = appointmentBaseTime.Start;
            order.EndTime = appointmentBaseTime.End;

            _context.Order.Add(order);
            _context.SaveChanges();
            var clinic = _context.Clinic.First();
            var doctor = _context.Doctor.First();
            var patient = _context.Patient.First();

            var request = new AddBookingRequest
            {
                StartTime = order.StartTime.AddHours(2),
                EndTime = order.EndTime.AddHours(2),
                DoctorId = doctor.Id,
                PatientId = patient.Id
            };

            var expected = new Order
            {
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                DoctorId = request.DoctorId,
                PatientId = request.PatientId,
                Doctor = doctor,
                Patient = patient,
                SurgeryType = (int)clinic.SurgeryType,
                IsCancelled = false
            };

            //act
            _bookingService.AddBooking(request);

            //assert
            _context.Order.Should().ContainEquivalentOf(expected, options => options.Excluding(order => order.Id));
        }

        [Test]
        public void GetPatientNextAppointment_NoOrdersInDb_ThrowsInvalidOperationException()
        {
            //arange
            var identificationNumber = 100;

            //act
            var exception = Assert.Throws<InvalidOperationException>(() => _bookingService.GetPatientNextAppointment(identificationNumber));

            //assert
            exception.Should().NotBeNull();
        }

        [Test]
        public void GetPatientNextAppointment_NoOrdersForPatient_ThrowsInvalidOperationException()
        {
            //arange
            var order = _fixture.Create<Order>();
            order.IsCancelled = false;
            order.StartTime = appointmentBaseTime.Start;
            order.EndTime = appointmentBaseTime.End;

            _context.Order.Add(order);
            _context.SaveChanges();

            var identificationNumber = order.PatientId != 111 ? 111 : 222;

            //act
            var exception = Assert.Throws<InvalidOperationException>(() => _bookingService.GetPatientNextAppointment(identificationNumber));

            //assert
            exception.Should().NotBeNull();
        }

        [Test]
        public void GetPatientNextAppointment_ReturnsCorrectOrder()
        {
            //arange
            var order = _fixture.Create<Order>();
            order.IsCancelled = false;
            order.StartTime = appointmentBaseTime.Start;
            order.EndTime = appointmentBaseTime.End;

            _context.Order.Add(order);
            _context.SaveChanges();

            var order2 = new Order {
                    IsCancelled = false,
                    StartTime = order.StartTime.AddHours(2),
                    EndTime = order.EndTime.AddHours(2),
                    DoctorId = order.DoctorId,
                    PatientId = order.PatientId
                };

            var orderInThePast = new Order
            {
                IsCancelled = false,
                StartTime = DateTime.Now.AddHours(-1),
                EndTime = DateTime.Now.AddHours(-1).AddMinutes(15),
                DoctorId = order.DoctorId,
                PatientId = order.PatientId
            };

            _context.Order.AddRange(order2, orderInThePast);
            _context.SaveChanges();


            var identificationNumber = order.PatientId;

            var expected = new GetPatientNextAppointmentResponse()
            {
                Id = order.Id,
                DoctorId = order.DoctorId,
                StartTime = order.StartTime,
                EndTime = order.EndTime
            };

            //act
            var result = _bookingService.GetPatientNextAppointment(identificationNumber);

            //assert
            result.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void CancelBooking_NoOrders_ThrowsInvalidOperationException()
        {
            //arange
            var request = new CancelBookingRequest();

            //act
            var exception = Assert.Throws<InvalidOperationException>(() => _bookingService.CancelBooking(request));

            //assert
            exception.Should().NotBeNull();
        }

        [Test]
        public void CancelBooking_UnexistingOrderId_ThrowsInvalidOperationException()
        {
            //arange
            var order = _fixture.Create<Order>();
            order.IsCancelled = false;
            order.StartTime = appointmentBaseTime.Start;
            order.EndTime = appointmentBaseTime.End;

            _context.Order.Add(order);
            _context.SaveChanges();

            var request = new CancelBookingRequest
            {
                Id = new Guid()
            };
            
            //act
            var exception = Assert.Throws<InvalidOperationException>(() => _bookingService.CancelBooking(request));

            //assert
            exception.Should().NotBeNull();
        }

        [Test]
        public void CancelBooking_Succeded()
        {
            //arange
            var order = _fixture.Create<Order>();
            order.IsCancelled = false;
            order.StartTime = appointmentBaseTime.Start;
            order.EndTime = appointmentBaseTime.End;

            _context.Order.Add(order);
            _context.SaveChanges();

            var request = new CancelBookingRequest
            {
                Id = order.Id
            };

            //act
            _bookingService.CancelBooking(request);

            //assert
            order.IsCancelled.Should().BeTrue();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
        }
    }
}
