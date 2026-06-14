using System.Net;
using System.Net.Http;
using System.Text.Json;
using CouponCalculator.Engine;
using CouponCalculator.Models;
using CouponCalculator.Services;

namespace CouponCalculator.Examples;

public static class RealIntegrationWithErrorHandling
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     优惠券SDK - 真实业务联调与异常处理示例                     ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        await RunWithErrorHandling();
    }

    public static async Task RunWithErrorHandling()
    {
        var integration = new RobustCouponIntegration();

        await Scenario1_NormalFlow(integration);
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        await Scenario2_InterfaceTimeout(integration);
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        await Scenario3_MissingFields(integration);
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        await Scenario4_CouponStatusChanged(integration);
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        await Scenario5_PartialFailure(integration);
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        await Scenario6_BatchProcessingWithRetry(integration);
        Console.WriteLine("\n" + new string('=', 70) + "\n");
        
        await Scenario7_RetryRecovery(integration);
        Console.WriteLine("\n" + new string('=', 70) + "\n");
        
        await Scenario8_MultiSystemInconsistency(integration);
        Console.WriteLine("\n" + new string('=', 70) + "\n");
        
        await Scenario9_FieldMappingValidation(integration);
    }

    private static async Task Scenario1_NormalFlow(RobustCouponIntegration integration)
    {
        Console.WriteLine("【场景1: 正常流程】\n");

        var request = new CouponCalculationRequest
        {
            OrderId = "ORDER-001",
            UserId = "USER-123",
            StoreId = "STORE-SH-001",
            ChannelId = "APP",
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = "SKU-001", Price = 100m, Quantity = 2, CategoryId = "CATE-001" },
                new OrderItemDto { ProductId = "SKU-002", Price = 50m, Quantity = 1, CategoryId = "CATE-002" }
            },
            MemberId = "MB-001",
            ShippingInfo = new ShippingDto { Weight = 1m, Province = "上海" }
        };

        var result = await integration.CalculateWithFullHandling(request);

        if (result.Success)
        {
            Console.WriteLine("✓ 计算成功");
            Console.WriteLine($"  原始金额: ¥{result.Data.OriginalAmount:F2}");
            Console.WriteLine($"  优惠金额: ¥{result.Data.TotalDiscount:F2}");
            Console.WriteLine($"  最终金额: ¥{result.Data.FinalAmount:F2}");
            Console.WriteLine($"  应用券数: {result.Data.AppliedCoupons.Count}");
        }
        else
        {
            Console.WriteLine($"✗ 计算失败: {result.ErrorMessage}");
        }
    }

    private static async Task Scenario2_InterfaceTimeout(RobustCouponIntegration integration)
    {
        Console.WriteLine("【场景2: 接口超时处理】\n");

        var request = new CouponCalculationRequest
        {
            OrderId = "ORDER-002",
            UserId = "USER-456",
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = "SKU-001", Price = 200m, Quantity = 1 }
            }
        };

        var result = await integration.CalculateWithFullHandling(request, new IntegrationOptions
        {
            TimeoutSeconds = 1,
            EnableFallback = true,
            EnableCircuitBreaker = true
        });

        Console.WriteLine($"请求超时处理结果: {result.Status}");
        if (result.Success)
        {
            Console.WriteLine("✓ 使用降级方案成功");
            Console.WriteLine($"  降级原因: {result.FallbackReason}");
        }
    }

    private static async Task Scenario3_MissingFields(RobustCouponIntegration integration)
    {
        Console.WriteLine("【场景3: 字段缺失处理】\n");

        var request = new CouponCalculationRequest
        {
            OrderId = "ORDER-003",
            UserId = "USER-789",
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = "SKU-001", Price = 150m, Quantity = 1 }
            }
        };

        var result = await integration.CalculateWithFullHandling(request);

        if (result.ValidationResult != null)
        {
            Console.WriteLine("✓ 字段验证结果:");
            if (result.ValidationResult.MissingFields.Any())
            {
                Console.WriteLine("  缺失字段:");
                foreach (var field in result.ValidationResult.MissingFields)
                {
                    Console.WriteLine($"    - {field}");
                }
            }
            if (result.ValidationResult.Warnings.Any())
            {
                Console.WriteLine("  警告信息:");
                foreach (var warning in result.ValidationResult.Warnings)
                {
                    Console.WriteLine($"    ⚠ {warning}");
                }
            }
        }
    }

    private static async Task Scenario4_CouponStatusChanged(RobustCouponIntegration integration)
    {
        Console.WriteLine("【场景4: 券状态变化处理】\n");

        var request = new CouponCalculationRequest
        {
            OrderId = "ORDER-004",
            UserId = "USER-101",
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = "SKU-001", Price = 300m, Quantity = 1 }
            },
            CouponIds = new List<string> { "COUPON-001", "COUPON-002", "COUPON-003" }
        };

        var result = await integration.CalculateWithFullHandling(request);

        if (result.CouponStatusChanges.Any())
        {
            Console.WriteLine("✓ 券状态变化检测:");
            foreach (var change in result.CouponStatusChanges)
            {
                var icon = change.OldStatus == change.NewStatus ? "→" : "⚠";
                Console.WriteLine($"  {icon} {change.CouponId}: {change.OldStatus} → {change.NewStatus}");
                if (!string.IsNullOrEmpty(change.Reason))
                    Console.WriteLine($"    原因: {change.Reason}");
            }
        }
    }

    private static async Task Scenario5_PartialFailure(RobustCouponIntegration integration)
    {
        Console.WriteLine("【场景5: 部分失败处理】\n");

        var request = new CouponCalculationRequest
        {
            OrderId = "ORDER-005",
            UserId = "USER-202",
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = "SKU-001", Price = 100m, Quantity = 1 },
                new OrderItemDto { ProductId = "SKU-002", Price = 200m, Quantity = 1 }
            },
            CouponIds = new List<string> { "COUPON-004", "COUPON-005", "COUPON-006" }
        };

        var result = await integration.CalculateWithFullHandling(request);

        if (result.PartialSuccess)
        {
            Console.WriteLine("⚠ 部分成功:");
            Console.WriteLine($"  成功项: {result.SuccessCount}/{result.TotalCount}");
            if (result.FailedItems.Any())
            {
                Console.WriteLine("  失败项:");
                foreach (var failed in result.FailedItems)
                {
                    Console.WriteLine($"    - {failed.ItemName}: {failed.Error}");
                }
            }
        }
    }

    private static async Task Scenario6_BatchProcessingWithRetry(RobustCouponIntegration integration)
    {
        Console.WriteLine("【场景6: 批量处理与重试】\n");

        var requests = Enumerable.Range(1, 10).Select(i => new CouponCalculationRequest
        {
            OrderId = $"BATCH-ORDER-{i}",
            UserId = $"USER-{i}",
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = "SKU-001", Price = 100m + i * 10, Quantity = 1 }
            }
        }).ToList();

        var batchResult = await integration.ProcessBatchWithRetry(requests, new BatchOptions
        {
            MaxConcurrency = 3,
            RetryCount = 2,
            RetryDelayMs = 100
        });

        Console.WriteLine($"批量处理结果:");
        Console.WriteLine($"  总请求数: {batchResult.TotalCount}");
        Console.WriteLine($"  成功: {batchResult.SuccessCount}");
        Console.WriteLine($"  失败: {batchResult.FailedCount}");
        Console.WriteLine($"  重试次数: {batchResult.TotalRetries}");
        Console.WriteLine($"  总耗时: {batchResult.TotalTimeMs}ms");
    }
    
    private static async Task Scenario7_RetryRecovery(RobustCouponIntegration integration)
    {
        Console.WriteLine("【场景7: 重试后恢复正常】\n");
        
        var request = new CouponCalculationRequest
        {
            OrderId = "ORDER-RETRY-001",
            UserId = "USER-RETRY",
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = "SKU-001", Price = 150m, Quantity = 1 }
            },
            CouponIds = new List<string> { "COUPON-RETRY-001" }
        };
        
        Console.WriteLine("模拟第一次请求失败...");
        var failResult = await integration.CalculateWithRetrySimulation(request, simulateFirstFailure: true);
        Console.WriteLine($"  第一次结果: {failResult.Status}");
        Console.WriteLine($"  错误信息: {failResult.ErrorMessage}");
        Console.WriteLine($"  重试次数: {failResult.RetryCount}");
        
        Console.WriteLine("\n模拟重试后成功...");
        var successResult = await integration.CalculateWithRetrySimulation(request, simulateFirstFailure: false);
        Console.WriteLine($"  重试结果: {successResult.Status}");
        if (successResult.Success)
        {
            Console.WriteLine($"  ✓ 最终成功");
            Console.WriteLine($"    原始金额: ¥{successResult.Data.OriginalAmount:F2}");
            Console.WriteLine($"    优惠金额: ¥{successResult.Data.TotalDiscount:F2}");
            Console.WriteLine($"    最终金额: ¥{successResult.Data.FinalAmount:F2}");
        }
        
        Console.WriteLine("\n重试恢复流程说明:");
        Console.WriteLine("  1. 首次请求失败时记录错误和重试信息");
        Console.WriteLine("  2. 根据错误类型决定是否重试");
        Console.WriteLine("  3. 重试时使用相同参数重新发起请求");
        Console.WriteLine("  4. 成功后对比前后结果，确认一致性");
    }
    
    private static async Task Scenario8_MultiSystemInconsistency(RobustCouponIntegration integration)
    {
        Console.WriteLine("【场景8: 多系统返回结果不一致】\n");
        
        var request = new CouponCalculationRequest
        {
            OrderId = "ORDER-MULTI-001",
            UserId = "USER-MULTI",
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = "SKU-001", Price = 200m, Quantity = 2 }
            },
            CouponIds = new List<string> { "COUPON-MULTI-001", "COUPON-MULTI-002" }
        };
        
        Console.WriteLine("模拟多系统计算结果对比...");
        
        var localResult = await integration.CalculateWithFullHandling(request);
        var paymentSystemResult = integration.SimulatePaymentSystemCalculation(request);
        var orderSystemResult = integration.SimulateOrderSystemCalculation(request);
        
        Console.WriteLine("\n各系统计算结果:");
        Console.WriteLine($"  本地SDK: ¥{localResult.Data?.FinalAmount ?? 0:F2}");
        Console.WriteLine($"  支付系统: ¥{paymentSystemResult.FinalAmount:F2}");
        Console.WriteLine($"  订单系统: ¥{orderSystemResult.FinalAmount:F2}");
        
        var inconsistency = integration.DetectInconsistency(localResult, paymentSystemResult, orderSystemResult);
        
        if (inconsistency.HasInconsistency)
        {
            Console.WriteLine("\n⚠ 检测到系统间不一致:");
            Console.WriteLine($"  最大差异: ¥{inconsistency.MaxDifference:F2}");
            Console.WriteLine($"  差异来源: {inconsistency.InconsistencySource}");
            
            Console.WriteLine("\n不一致处理建议:");
            foreach (var suggestion in inconsistency.Suggestions)
            {
                Console.WriteLine($"  · {suggestion}");
            }
            
            var reconciliation = integration.GenerateReconciliationExplanation(localResult, paymentSystemResult, orderSystemResult);
            Console.WriteLine("\n对账说明:");
            Console.WriteLine(reconciliation);
        }
        else
        {
            Console.WriteLine("\n✓ 各系统结果一致");
        }
        
        Console.WriteLine("\n多系统不一致处理流程:");
        Console.WriteLine("  1. 同时调用多个系统获取计算结果");
        Console.WriteLine("  2. 对比各系统返回的最终金额");
        Console.WriteLine("  3. 检测差异并定位不一致来源");
        Console.WriteLine("  4. 生成对账说明供运营复盘");
        Console.WriteLine("  5. 根据差异大小决定是否需要人工介入");
    }
    
    private static async Task Scenario9_FieldMappingValidation(RobustCouponIntegration integration)
    {
        Console.WriteLine("【场景9: 字段映射验证】\n");
        Console.WriteLine("模拟业务系统改完字段映射后的验证流程...\n");
        
        var testCases = new List<FieldMappingTestCase>
        {
            new FieldMappingTestCase
            {
                CaseName = "商品ID字段映射",
                OldFieldName = "product_id",
                NewFieldName = "ProductId",
                TestValue = "SKU-MAP-001"
            },
            new FieldMappingTestCase
            {
                CaseName = "价格字段映射",
                OldFieldName = "price",
                NewFieldName = "Price",
                TestValue = "100.50"
            },
            new FieldMappingTestCase
            {
                CaseName = "数量字段映射",
                OldFieldName = "qty",
                NewFieldName = "Quantity",
                TestValue = "2"
            },
            new FieldMappingTestCase
            {
                CaseName = "会员等级字段映射",
                OldFieldName = "member_level",
                NewFieldName = "MemberLevel",
                TestValue = "VIP"
            }
        };
        
        Console.WriteLine("字段映射验证结果:");
        foreach (var testCase in testCases)
        {
            var validation = integration.ValidateFieldMapping(testCase);
            Console.WriteLine($"  {testCase.CaseName}:");
            Console.WriteLine($"    旧字段: {testCase.OldFieldName} → 新字段: {testCase.NewFieldName}");
            Console.WriteLine($"    测试值: {testCase.TestValue}");
            Console.WriteLine($"    验证结果: {validation.IsValid ? "✓ 通过" : "✗ 失败"}");
            if (!validation.IsValid)
            {
                Console.WriteLine($"    错误: {validation.ErrorMessage}");
            }
            Console.WriteLine($"    映射后值: {validation.MappedValue}");
        }
        
        Console.WriteLine("\n完整流程验证:");
        var request = new CouponCalculationRequest
        {
            OrderId = "ORDER-MAP-VALIDATION",
            UserId = "USER-MAP",
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = "SKU-MAP-001", Price = 100.50m, Quantity = 2 }
            },
            MemberLevel = MemberLevel.VIP
        };
        
        var result = await integration.CalculateWithFullHandling(request);
        
        Console.WriteLine("\n验证计算结果:");
        if (result.ValidationResult?.MissingFields?.Any() == true)
        {
            Console.WriteLine("  ⚠ 存在缺失字段:");
            foreach (var field in result.ValidationResult.MissingFields)
            {
                Console.WriteLine($"    - {field}");
            }
        }
        else
        {
            Console.WriteLine("  ✓ 所有字段映射正确");
            Console.WriteLine($"    计算成功: {result.Success}");
            Console.WriteLine($"    最终金额: ¥{result.Data?.FinalAmount ?? 0:F2}");
        }
        
        Console.WriteLine("\n字段映射验证流程说明:");
        Console.WriteLine("  1. 定义字段映射规则（旧字段名 → 新字段名）");
        Console.WriteLine("  2. 对每个字段进行单独验证");
        Console.WriteLine("  3. 验证映射后的值是否符合预期类型");
        Console.WriteLine("  4. 使用完整请求验证整体流程");
        Console.WriteLine("  5. 检查是否有缺失字段或类型不匹配");
    }
}

public class RobustCouponIntegration
{
    private readonly CouponCalculatorEngine _engine;
    private readonly ItemLevelAllocationService _allocationService;
    private readonly ReconciliationService _reconciliationService;
    private readonly TemplateManagementService _templateService;

    public RobustCouponIntegration()
    {
        _engine = new CouponCalculatorEngine();
        _allocationService = new ItemLevelAllocationService();
        _reconciliationService = new ReconciliationService();
        _templateService = new TemplateManagementService();
    }

    public async Task<CouponCalculationResponse> CalculateWithFullHandling(
        CouponCalculationRequest request,
        IntegrationOptions options = null)
    {
        options ??= new IntegrationOptions();
        var response = new CouponCalculationResponse { RequestId = request.OrderId };

        var validationResult = ValidateRequest(request);
        response.ValidationResult = validationResult;

        if (!validationResult.IsValid)
        {
            response.Status = ResponseStatus.ValidationError;
            response.ErrorMessage = string.Join("; ", validationResult.Errors);
            return response;
        }

        if (validationResult.Warnings.Any())
        {
            response.Warnings.AddRange(validationResult.Warnings);
        }

        try
        {
            var context = await BuildOrderContextAsync(request, options.TimeoutSeconds);
            
            var couponStatusChanges = await CheckCouponStatusAsync(request.CouponIds, options.TimeoutSeconds);
            response.CouponStatusChanges = couponStatusChanges;

            var coupons = await LoadCouponsAsync(request.CouponIds, context.OriginalAmount, options.TimeoutSeconds);
            var availableCoupons = coupons.Where(c => c.IsValid()).ToList();

            var expiredOrInvalid = coupons.Except(availableCoupons).ToList();
            foreach (var coupon in expiredOrInvalid)
            {
                response.CouponStatusChanges.Add(new CouponStatusChange
                {
                    CouponId = coupon.CouponId,
                    OldStatus = "Available",
                    NewStatus = "Expired",
                    Reason = "优惠券已过期或无效"
                });
            }

            var result = _engine.CalculateOptimalEnhanced(context, availableCoupons);

            response.Data = new CouponCalculationData
            {
                OriginalAmount = result.OriginalAmount,
                TotalDiscount = result.RecommendedPlan?.TotalDiscountAmount ?? 0,
                FinalAmount = result.RecommendedPlan?.FinalTotalAmount ?? context.OriginalAmount,
                AppliedCoupons = result.RecommendedPlan?.AppliedCoupons?.Select(c => new AppliedCouponInfo
                {
                    CouponId = c.CouponId,
                    CouponCode = c.CouponCode,
                    DiscountAmount = c.DiscountAmount,
                    Reason = c.Reason
                }).ToList() ?? new List<AppliedCouponInfo>(),
                CandidatePlans = result.CandidatePlans?.Select(p => new CandidatePlanInfo
                {
                    PlanName = p.PlanName,
                    TotalDiscount = p.TotalDiscountAmount,
                    FinalAmount = p.FinalTotalAmount,
                    CouponCount = p.AppliedCoupons.Count
                }).ToList() ?? new List<CandidatePlanInfo>()
            };

            response.Success = true;
            response.Status = ResponseStatus.Success;

            SaveCalculationTemplate(request, response);
        }
        catch (TimeoutException)
        {
            response = HandleTimeout(response, options);
        }
        catch (HttpRequestException ex)
        {
            response = HandleHttpError(response, ex, options);
        }
        catch (Exception ex)
        {
            response = HandleGeneralError(response, ex, options);
        }

        return response;
    }

    public async Task<BatchProcessingResult> ProcessBatchWithRetry(
        List<CouponCalculationRequest> requests,
        BatchOptions options)
    {
        var result = new BatchProcessingResult { TotalCount = requests.Count };
        var startTime = DateTime.UtcNow;

        var semaphore = new SemaphoreSlim(options.MaxConcurrency);
        var tasks = new List<Task>();

        foreach (var request in requests)
        {
            await semaphore.WaitAsync();
            
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var retryCount = 0;
                    while (retryCount <= options.RetryCount)
                    {
                        try
                        {
                            var response = await CalculateWithFullHandling(request);
                            if (response.Success)
                            {
                                lock (result)
                                {
                                    result.SuccessCount++;
                                    result.SuccessItems.Add(new BatchSuccessItem
                                    {
                                        OrderId = request.OrderId,
                                        Result = response
                                    });
                                }
                                break;
                            }
                            else
                            {
                                retryCount++;
                                if (retryCount <= options.RetryCount)
                                {
                                    result.TotalRetries++;
                                    await Task.Delay(options.RetryDelayMs);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            retryCount++;
                            if (retryCount <= options.RetryCount)
                            {
                                result.TotalRetries++;
                                await Task.Delay(options.RetryDelayMs);
                            }
                        }
                    }

                    if (retryCount > options.RetryCount)
                    {
                        lock (result)
                        {
                            result.FailedCount++;
                            result.FailedItems.Add(new BatchFailedItem
                            {
                                OrderId = request.OrderId,
                                Error = "超过最大重试次数"
                            });
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);

        result.TotalTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
        return result;
    }

    private RequestValidationResult ValidateRequest(CouponCalculationRequest request)
    {
        var result = new RequestValidationResult { IsValid = true };

        if (string.IsNullOrEmpty(request.OrderId))
        {
            result.IsValid = false;
            result.MissingFields.Add("OrderId");
        }

        if (request.Items == null || !request.Items.Any())
        {
            result.IsValid = false;
            result.MissingFields.Add("Items");
        }
        else
        {
            foreach (var item in request.Items.Where(i => string.IsNullOrEmpty(i.ProductId)))
            {
                result.IsValid = false;
                result.MissingFields.Add($"Items[{request.Items.IndexOf(item)}].ProductId");
            }

            foreach (var item in request.Items.Where(i => i.Price <= 0))
            {
                result.Warnings.Add($"商品 {item.ProductId} 价格为0或负数");
            }
        }

        if (string.IsNullOrEmpty(request.UserId))
        {
            result.Warnings.Add("UserId缺失，部分功能可能不可用");
        }

        return result;
    }

    private async Task<OrderContext> BuildOrderContextAsync(CouponCalculationRequest request, int timeoutSeconds)
    {
        await Task.Delay(50);

        var context = _engine.CreateOrderContext(request.OrderId);

        foreach (var item in request.Items ?? new List<OrderItemDto>())
        {
            context.AddItem(new OrderItem
            {
                ProductId = item.ProductId ?? $"UNKNOWN-{Guid.NewGuid()}",
                ProductName = item.ProductName ?? "未知商品",
                Price = item.Price,
                Quantity = item.Quantity,
                CategoryId = item.CategoryId ?? "DEFAULT"
            });
        }

        if (!string.IsNullOrEmpty(request.MemberId))
        {
            context.Member = new MemberInfo
            {
                MemberId = request.MemberId,
                Level = request.MemberLevel ?? MemberLevel.Normal
            };
        }

        if (request.ShippingInfo != null)
        {
            context.FreightAmount = CalculateFreight(request.ShippingInfo);
        }

        context.ExtendedData["StoreId"] = request.StoreId ?? "";
        context.ExtendedData["ChannelId"] = request.ChannelId ?? "";
        context.ExtendedData["UserId"] = request.UserId ?? "";

        return context;
    }

    private decimal CalculateFreight(ShippingDto shipping)
    {
        if (shipping.Weight <= 1)
            return 10m;
        return 10m + (decimal)Math.Ceiling((double)(shipping.Weight - 1)) * 5m;
    }

    private async Task<List<Coupon>> LoadCouponsAsync(List<string> couponIds, decimal orderAmount, int timeoutSeconds)
    {
        await Task.Delay(30);

        if (couponIds == null || !couponIds.Any())
            return new List<Coupon>();

        return couponIds.Select(id => new Coupon
        {
            CouponId = id,
            CouponCode = $"CODE-{id}",
            Type = CouponType.AmountOff,
            DiscountValue = 20m,
            MinOrderAmount = 100m,
            ValidFrom = DateTime.UtcNow.AddDays(-30),
            ValidTo = DateTime.UtcNow.AddDays(30)
        }).ToList();
    }

    private async Task<List<CouponStatusChange>> CheckCouponStatusAsync(List<string> couponIds, int timeoutSeconds)
    {
        await Task.Delay(20);
        return new List<CouponStatusChange>();
    }

    private void SaveCalculationTemplate(CouponCalculationRequest request, CouponCalculationResponse response)
    {
        var snapshot = new TemplateSnapshot
        {
            OrderId = request.OrderId,
            SnapshotTime = DateTime.UtcNow
        };

        _templateService.CreateTemplate(
            $"Template-{request.OrderId}",
            "AutoGenerated",
            TemplateType.Order,
            snapshot,
            "System"
        );
    }

    private CouponCalculationResponse HandleTimeout(CouponCalculationResponse response, IntegrationOptions options)
    {
        response.Status = ResponseStatus.Timeout;
        response.ErrorMessage = "请求超时";

        if (options.EnableFallback)
        {
            response.Success = true;
            response.FallbackReason = "使用缓存的优惠方案作为降级处理";
            response.Data = new CouponCalculationData
            {
                OriginalAmount = 0,
                TotalDiscount = 0,
                FinalAmount = 0
            };
        }

        return response;
    }

    private CouponCalculationResponse HandleHttpError(CouponCalculationResponse response, HttpRequestException ex, IntegrationOptions options)
    {
        response.Status = ResponseStatus.NetworkError;
        response.ErrorMessage = $"网络错误: {ex.Message}";

        if (options.EnableFallback)
        {
            response.Success = true;
            response.FallbackReason = "网络异常，使用离线模式";
        }

        return response;
    }

    private CouponCalculationResponse HandleGeneralError(CouponCalculationResponse response, Exception ex, IntegrationOptions options)
    {
        response.Status = ResponseStatus.SystemError;
        response.ErrorMessage = $"系统错误: {ex.Message}";
        response.SystemError = new SystemErrorInfo
        {
            ErrorType = ex.GetType().Name,
            StackTrace = ex.StackTrace ?? "",
            Timestamp = DateTime.UtcNow
        };

        return response;
    }
    
    public async Task<RetrySimulationResult> CalculateWithRetrySimulation(
        CouponCalculationRequest request,
        bool simulateFirstFailure)
    {
        var result = new RetrySimulationResult
        {
            OrderId = request.OrderId,
            RetryCount = 0
        };
        
        if (simulateFirstFailure)
        {
            result.Status = "Failed";
            result.ErrorMessage = "模拟首次请求失败: 网络超时";
            result.RetryCount = 1;
            result.Success = false;
            return result;
        }
        
        var response = await CalculateWithFullHandling(request);
        result.Status = response.Status.ToString();
        result.Success = response.Success;
        result.ErrorMessage = response.ErrorMessage ?? "";
        result.RetryCount = 1;
        
        if (response.Success && response.Data != null)
        {
            result.Data = response.Data;
        }
        
        return result;
    }
    
    public SimulatedSystemResult SimulatePaymentSystemCalculation(CouponCalculationRequest request)
    {
        var totalAmount = request.Items?.Sum(i => i.Price * i.Quantity) ?? 0;
        var discount = totalAmount * 0.12m;
        
        return new SimulatedSystemResult
        {
            SystemName = "支付系统",
            FinalAmount = totalAmount - discount + 5m,
            DiscountAmount = discount,
            CalculationTime = DateTime.UtcNow,
            AppliedCoupons = request.CouponIds?.ToList() ?? new List<string>(),
            Notes = "支付系统计算结果，包含5元手续费调整"
        };
    }
    
    public SimulatedSystemResult SimulateOrderSystemCalculation(CouponCalculationRequest request)
    {
        var totalAmount = request.Items?.Sum(i => i.Price * i.Quantity) ?? 0;
        var discount = totalAmount * 0.10m;
        
        return new SimulatedSystemResult
        {
            SystemName = "订单系统",
            FinalAmount = totalAmount - discount,
            DiscountAmount = discount,
            CalculationTime = DateTime.UtcNow,
            AppliedCoupons = request.CouponIds?.ToList() ?? new List<string>(),
            Notes = "订单系统计算结果，标准折扣"
        };
    }
    
    public InconsistencyResult DetectInconsistency(
        CouponCalculationResponse localResult,
        SimulatedSystemResult paymentResult,
        SimulatedSystemResult orderResult)
    {
        var localAmount = localResult.Data?.FinalAmount ?? 0;
        var paymentAmount = paymentResult.FinalAmount;
        var orderAmount = orderResult.FinalAmount;
        
        var differences = new List<decimal>
        {
            Math.Abs(localAmount - paymentAmount),
            Math.Abs(localAmount - orderAmount),
            Math.Abs(paymentAmount - orderAmount)
        };
        
        var maxDiff = differences.Max();
        
        var result = new InconsistencyResult
        {
            HasInconsistency = maxDiff > 0.01m,
            MaxDifference = maxDiff,
            LocalAmount = localAmount,
            PaymentAmount = paymentAmount,
            OrderAmount = orderAmount
        };
        
        if (result.HasInconsistency)
        {
            if (Math.Abs(localAmount - paymentAmount) > Math.Abs(localAmount - orderAmount))
            {
                result.InconsistencySource = "本地SDK与支付系统差异最大";
            }
            else
            {
                result.InconsistencySource = "本地SDK与订单系统差异最大";
            }
            
            result.Suggestions = new List<string>
            {
                "检查各系统使用的规则版本是否一致",
                "核对优惠券计算口径是否有差异",
                "确认是否存在手续费或额外调整项",
                "建议以订单系统结果为准进行对账"
            };
        }
        
        return result;
    }
    
    public string GenerateReconciliationExplanation(
        CouponCalculationResponse localResult,
        SimulatedSystemResult paymentResult,
        SimulatedSystemResult orderResult)
    {
        var lines = new List<string>();
        
        lines.Add("【多系统对账说明】");
        lines.Add($"本地SDK: ¥{localResult.Data?.FinalAmount ?? 0:F2}");
        lines.Add($"支付系统: ¥{paymentResult.FinalAmount:F2} ({paymentResult.Notes})");
        lines.Add($"订单系统: ¥{orderResult.FinalAmount:F2} ({orderResult.Notes})");
        lines.Add("");
        lines.Add("差异分析:");
        lines.Add($"  本地-支付: ¥{Math.Abs((localResult.Data?.FinalAmount ?? 0) - paymentResult.FinalAmount):F2}");
        lines.Add($"  本地-订单: ¥{Math.Abs((localResult.Data?.FinalAmount ?? 0) - orderResult.FinalAmount):F2}");
        lines.Add($"  支付-订单: ¥{Math.Abs(paymentResult.FinalAmount - orderResult.FinalAmount):F2}");
        lines.Add("");
        lines.Add("建议处理方式:");
        lines.Add("  1. 以订单系统结果作为基准");
        lines.Add("  2. 支付系统差异可能包含手续费调整");
        lines.Add("  3. 如差异超过阈值，需人工复核");
        
        return string.Join(Environment.NewLine, lines);
    }
    
    public FieldMappingValidationResult ValidateFieldMapping(FieldMappingTestCase testCase)
    {
        var result = new FieldMappingValidationResult
        {
            CaseName = testCase.CaseName,
            OldFieldName = testCase.OldFieldName,
            NewFieldName = testCase.NewFieldName
        };
        
        try
        {
            switch (testCase.NewFieldName)
            {
                case "ProductId":
                    result.MappedValue = testCase.TestValue;
                    result.IsValid = !string.IsNullOrEmpty(testCase.TestValue);
                    break;
                case "Price":
                    var price = decimal.Parse(testCase.TestValue);
                    result.MappedValue = price.ToString("F2");
                    result.IsValid = price > 0;
                    break;
                case "Quantity":
                    var qty = int.Parse(testCase.TestValue);
                    result.MappedValue = qty.ToString();
                    result.IsValid = qty > 0;
                    break;
                case "MemberLevel":
                    result.MappedValue = testCase.TestValue;
                    result.IsValid = Enum.TryParse<MemberLevel>(testCase.TestValue, out _);
                    break;
                default:
                    result.IsValid = false;
                    result.ErrorMessage = "未知的字段类型";
                    break;
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = ex.Message;
            result.MappedValue = "映射失败";
        }
        
        return result;
    }
}

public class CouponCalculationRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
    public string MemberId { get; set; } = string.Empty;
    public MemberLevel? MemberLevel { get; set; }
    public ShippingDto ShippingInfo { get; set; }
    public List<string> CouponIds { get; set; } = new();
}

public class OrderItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string CategoryId { get; set; } = string.Empty;
}

public class ShippingDto
{
    public decimal Weight { get; set; }
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class CouponCalculationResponse
{
    public string RequestId { get; set; }
    public bool Success { get; set; }
    public ResponseStatus Status { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string FallbackReason { get; set; } = string.Empty;
    public RequestValidationResult ValidationResult { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<CouponStatusChange> CouponStatusChanges { get; set; } = new();
    public CouponCalculationData Data { get; set; }
    public bool PartialSuccess { get; set; }
    public int SuccessCount { get; set; }
    public int TotalCount { get; set; }
    public List<BatchFailedItem> FailedItems { get; set; } = new();
    public SystemErrorInfo SystemError { get; set; }
}

public enum ResponseStatus
{
    Success,
    ValidationError,
    Timeout,
    NetworkError,
    SystemError,
    PartialSuccess
}

public class RequestValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> MissingFields { get; set; } = new();
}

public class CouponStatusChange
{
    public string CouponId { get; set; } = string.Empty;
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class CouponCalculationData
{
    public decimal OriginalAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal FinalAmount { get; set; }
    public List<AppliedCouponInfo> AppliedCoupons { get; set; } = new();
    public List<CandidatePlanInfo> CandidatePlans { get; set; } = new();
}

public class AppliedCouponInfo
{
    public string CouponId { get; set; }
    public string CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Reason { get; set; }
}

public class CandidatePlanInfo
{
    public string PlanName { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal FinalAmount { get; set; }
    public int CouponCount { get; set; }
}

public class BatchSuccessItem
{
    public string OrderId { get; set; }
    public CouponCalculationResponse Result { get; set; }
}

public class BatchFailedItem
{
    public string OrderId { get; set; }
    public string Error { get; set; }
}

public class SystemErrorInfo
{
    public string ErrorType { get; set; }
    public string StackTrace { get; set; }
    public DateTime Timestamp { get; set; }
}

public class IntegrationOptions
{
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableFallback { get; set; } = true;
    public bool EnableCircuitBreaker { get; set; } = false;
    public int CircuitBreakerThreshold { get; set; } = 5;
}

public class BatchOptions
{
    public int MaxConcurrency { get; set; } = 5;
    public int RetryCount { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 200;
}

public class BatchProcessingResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalRetries { get; set; }
    public long TotalTimeMs { get; set; }
    public List<BatchSuccessItem> SuccessItems { get; set; } = new();
    public List<BatchFailedItem> FailedItems { get; set; } = new();
}

public class RetrySimulationResult
{
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public CouponCalculationData Data { get; set; }
}

public class SimulatedSystemResult
{
    public string SystemName { get; set; } = string.Empty;
    public decimal FinalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime CalculationTime { get; set; }
    public List<string> AppliedCoupons { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}

public class InconsistencyResult
{
    public bool HasInconsistency { get; set; }
    public decimal MaxDifference { get; set; }
    public decimal LocalAmount { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal OrderAmount { get; set; }
    public string InconsistencySource { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = new();
}

public class FieldMappingTestCase
{
    public string CaseName { get; set; } = string.Empty;
    public string OldFieldName { get; set; } = string.Empty;
    public string NewFieldName { get; set; } = string.Empty;
    public string TestValue { get; set; } = string.Empty;
}

public class FieldMappingValidationResult
{
    public string CaseName { get; set; } = string.Empty;
    public string OldFieldName { get; set; } = string.Empty;
    public string NewFieldName { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string MappedValue { get; set; } = string.Empty;
}