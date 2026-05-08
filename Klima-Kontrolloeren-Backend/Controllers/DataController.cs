using Microsoft.AspNetCore.Mvc;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;

namespace KlimaKontrolloerenBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly IDataService _dataService;
    public DataController(IDataService dataService)
    {
        _sensorService = sensorService;
    }