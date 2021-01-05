using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDR.PatientBooking.Data;
using PDR.PatientBooking.Data.Models;
using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.BookingServices.Validation;
using System;

namespace PDR.PatientBooking.Service.Tests.BookingServices.Validation
{
    public class AddBookingRequestValidatorTests
    {
        private IFixture _fixture;

        private PatientBookingContext _context;

        private AddBookingRequestValidator _addBookingRequestValidator;

        [SetUp]
        public void SetUp()
        {
            // Boilerplate
            _fixture = new Fixture();

            //Prevent fixture from generating from entity circular references 
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            // Mock setup
            _context = new PatientBookingContext(new DbContextOptionsBuilder<PatientBookingContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

            // Mock default
            SetupMockDefaults();

            // Sut instantiation
            _addBookingRequestValidator = new AddBookingRequestValidator(
                _context
            );
        }

        private void SetupMockDefaults()
        {

        }

        [Test]
        public void ValidateRequest_AllChecksPass_ReturnsPassedValidationResult()
        {
            //arrange
            var request = GetValidRequest();

            //act
            var result = _addBookingRequestValidator.ValidateRequest(request);

            //assert
            result.PassedValidation.Should().BeTrue();
        }

        [Test]
        public void ValidateRequest_DateInPast_ReturnsFailedValidationResult()
        {
            //arrange
            var request = GetValidRequest();
            request.StartTime = DateTime.Now.AddMinutes(-30);

            //act
            var result = _addBookingRequestValidator.ValidateRequest(request);

            //assert
            result.PassedValidation.Should().BeFalse();
            result.Errors.Should().Contain("Invalid appointment start.");
        }

        [Test]
        public void ValidateRequest_TimeIsBusy_ReturnsFailedValidationResult()
        {
            //arrange
            var startTime = new DateTime(2021, 1, 11, 12, 30, 00);
            var endTime = startTime.AddMinutes(15);

            var order = GetOrder(startTime, endTime);

            _context.AddRange(order);
            _context.SaveChanges();

            var request = GetValidRequest();
            request.DoctorId = order.DoctorId;
            request.StartTime = startTime;
            request.EndTime = endTime;

            //act
            var result = _addBookingRequestValidator.ValidateRequest(request);

            //assert
            result.PassedValidation.Should().BeFalse();
            result.Errors.Should().Contain("Requested time is busy.");
        }

        [TestCase(15)]
        [TestCase(-15)]
        public void ValidateRequest_TimeIsBusy_RequestStartEndEqualsEndStartTime_ReturnsPassedValidationResult(int intervalInMinutes)
        {
            //arrange
            var startTime = new DateTime(2021, 1, 11, 12, 30, 00);
            var endTime = startTime.AddMinutes(15);

            var order = GetOrder(startTime, endTime);

            _context.AddRange(order);
            _context.SaveChanges();

            var request = GetValidRequest();
            request.DoctorId = order.DoctorId;
            request.StartTime = startTime.AddMinutes(intervalInMinutes);
            request.EndTime = endTime.AddMinutes(intervalInMinutes);

            //act
            var result = _addBookingRequestValidator.ValidateRequest(request);

            //assert
            result.PassedValidation.Should().BeTrue();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
        }

        private AddBookingRequest GetValidRequest()
        {
            var request = _fixture.Create<AddBookingRequest>();
            request.StartTime = DateTime.Now.AddHours(1);
            request.EndTime = DateTime.Now.AddHours(1).AddMinutes(30);

            return request;
        }

        private Order GetOrder(DateTime start, DateTime end)
        {
            var order = _fixture.Create<Order>();

            order.StartTime = start;
            order.EndTime = end;
            order.IsCancelled = false;

            return order;
        }
    }
}
