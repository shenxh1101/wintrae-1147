namespace CouponCalculator.Models;

public class TemplateCollaboration
{
    public string CollaborationId { get; set; } = Guid.NewGuid().ToString();
    public string TemplateId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    
    public SharedExplanation SharedExplanation { get; set; } = new();
    
    public List<RoleAnnotation> Annotations { get; set; } = new();
    public List<ProcessingConclusion> Conclusions { get; set; } = new();
    
    public CollaborationStatus Status { get; set; }
    
    public List<CollaborationHistory> History { get; set; } = new();
    
    public List<string> RelatedTemplateIds { get; set; } = new();
    
    public string GenerateCollaborationSummary()
    {
        var summary = new List<string>();
        
        summary.Add("╔══════════════════════════════════════════════════════════════════════╗");
        summary.Add("║              口径协作汇总                                            ║");
        summary.Add("╚══════════════════════════════════════════════════════════════════════╝");
        summary.Add($"协作ID: {CollaborationId}");
        summary.Add($"模板ID: {TemplateId}");
        summary.Add($"订单ID: {OrderId}");
        summary.Add($"状态: {Status}");
        summary.Add($"创建时间: {CreatedAt:yyyy-MM-dd HH:mm:ss}");
        summary.Add("");
        
        summary.Add("【共用口径说明】");
        summary.Add($"  原始金额: ¥{SharedExplanation.OriginalAmount:F2}");
        summary.Add($"  优惠金额: ¥{SharedExplanation.TotalDiscount:F2}");
        summary.Add($"  最终金额: ¥{SharedExplanation.FinalAmount:F2}");
        if (!string.IsNullOrEmpty(SharedExplanation.Summary))
            summary.Add($"  概要: {SharedExplanation.Summary}");
        summary.Add("");
        
        if (Annotations.Any())
        {
            summary.Add("【各角色备注】");
            foreach (var annotation in Annotations.OrderBy(a => a.CreatedAt))
            {
                var icon = annotation.Role switch
                {
                    CollaborationRole.CustomerService => "客服",
                    CollaborationRole.Operations => "运营",
                    CollaborationRole.Finance => "财务",
                    CollaborationRole.Technical => "技术",
                    _ => "其他"
                };
                summary.Add($"  [{icon}] {annotation.Author}: {annotation.Content}");
                if (!string.IsNullOrEmpty(annotation.InternalNote))
                    summary.Add($"    内部备注: {annotation.InternalNote}");
            }
            summary.Add("");
        }
        
        if (Conclusions.Any())
        {
            summary.Add("【处理结论】");
            foreach (var conclusion in Conclusions)
            {
                summary.Add($"  {conclusion.Role}: {conclusion.ConclusionType}");
                summary.Add($"    内容: {conclusion.Content}");
                summary.Add($"    时间: {conclusion.ConcludedAt:yyyy-MM-dd HH:mm:ss}");
            }
            summary.Add("");
        }
        
        summary.Add("╚══════════════════════════════════════════════════════════════════════╝");
        
        return string.Join(Environment.NewLine, summary);
    }
}

public enum CollaborationStatus
{
    Draft,
    InProgress,
    Completed,
    Archived,
    Escalated
}

public class SharedExplanation
{
    public string ExplanationId { get; set; } = Guid.NewGuid().ToString();
    
    public decimal OriginalAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal Freight { get; set; }
    
    public string Summary { get; set; } = string.Empty;
    
    public List<SharedDiscountItem> DiscountItems { get; set; } = new();
    public List<SharedCouponItem> CouponItems { get; set; } = new();
    
    public Dictionary<string, string> KeyPoints { get; set; } = new();
    
    public string CustomerVersion { get; set; } = string.Empty;
    public string InternalVersion { get; set; } = string.Empty;
    
    public string GenerateForCustomer()
    {
        var lines = new List<string>();
        lines.Add($"订单金额: ¥{OriginalAmount:F2}");
        lines.Add($"优惠减免: ¥{TotalDiscount:F2}");
        lines.Add($"应付金额: ¥{FinalAmount:F2}");
        
        if (DiscountItems.Any())
        {
            lines.Add("");
            lines.Add("优惠明细:");
            foreach (var item in DiscountItems)
            {
                lines.Add($"  · {item.Name}: -¥{item.Amount:F2}");
            }
        }
        
        if (!string.IsNullOrEmpty(Summary))
        {
            lines.Add("");
            lines.Add(Summary);
        }
        
        return string.Join(Environment.NewLine, lines);
    }
    
    public string GenerateForInternal()
    {
        var lines = new List<string>();
        lines.Add($"【内部口径】");
        lines.Add($"原始金额: ¥{OriginalAmount:F2}");
        lines.Add($"优惠总额: ¥{TotalDiscount:F2}");
        lines.Add($"运费: ¥{Freight:F2}");
        lines.Add($"应付: ¥{FinalAmount:F2}");
        
        if (CouponItems.Any())
        {
            lines.Add("");
            lines.Add("优惠券明细:");
            foreach (var item in CouponItems)
            {
                lines.Add($"  {item.CouponId}: ¥{item.Amount:F2} ({item.Type})");
                if (!string.IsNullOrEmpty(item.RuleReference))
                    lines.Add($"    规则引用: {item.RuleReference}");
            }
        }
        
        if (KeyPoints.Any())
        {
            lines.Add("");
            lines.Add("关键点:");
            foreach (var kvp in KeyPoints)
            {
                lines.Add($"  {kvp.Key}: {kvp.Value}");
            }
        }
        
        return string.Join(Environment.NewLine, lines);
    }
}

public class SharedDiscountItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class SharedCouponItem
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string RuleReference { get; set; } = string.Empty;
}

public class RoleAnnotation
{
    public string AnnotationId { get; set; } = Guid.NewGuid().ToString();
    public string TemplateId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    
    public CollaborationRole Role { get; set; }
    public string Author { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    public string InternalNote { get; set; } = string.Empty;
    
    public AnnotationType Type { get; set; }
    public AnnotationPriority Priority { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public List<string> RelatedIssues { get; set; } = new();
    public List<string> Attachments { get; set; } = new();
    
    public bool IsVisibleToCustomer { get; set; }
    public bool RequiresFollowUp { get; set; }
    
    public string? FollowUpAction { get; set; }
    public DateTime? FollowUpDeadline { get; set; }
}

public enum CollaborationRole
{
    CustomerService,
    Operations,
    Finance,
    Technical,
    Manager,
    External
}

public enum AnnotationType
{
    Note,
    Question,
    Answer,
    Clarification,
    Warning,
    Approval,
    Rejection,
    Escalation
}

public enum AnnotationPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public class ProcessingConclusion
{
    public string ConclusionId { get; set; } = Guid.NewGuid().ToString();
    public string TemplateId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    
    public CollaborationRole Role { get; set; }
    public string ConcludedBy { get; set; } = string.Empty;
    public string ConcludedById { get; set; } = string.Empty;
    
    public ConclusionType ConclusionType { get; set; }
    public string Content { get; set; } = string.Empty;
    
    public DateTime ConcludedAt { get; set; }
    
    public List<string> SupportingEvidence { get; set; } = new();
    public List<string> RelatedActions { get; set; } = new();
    
    public bool IsFinal { get; set; }
    public bool RequiresApproval { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public enum ConclusionType
{
    Confirmed,
    Adjusted,
    Refunded,
    Escalated,
    Closed,
    PendingReview,
    RequiresFollowUp
}

public class CollaborationHistory
{
    public string HistoryId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string PreviousValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}

public class TemplateAutoFill
{
    public string AutoFillId { get; set; } = Guid.NewGuid().ToString();
    public string TemplateId { get; set; } = string.Empty;
    
    public AutoFillTrigger Trigger { get; set; }
    public List<AutoFillCondition> Conditions { get; set; } = new();
    
    public AutoFillContent Content { get; set; } = new();
    
    public bool IsActive { get; set; }
    public int MatchCount { get; set; }
    public DateTime LastMatchedAt { get; set; }
    
    public decimal MatchScore { get; set; }
    
    public bool EvaluateMatch(OrderContextSnapshot orderSnapshot)
    {
        var matchScore = 0m;
        var totalConditions = Conditions.Count;
        
        foreach (var condition in Conditions)
        {
            if (EvaluateCondition(condition, orderSnapshot))
                matchScore += condition.Weight;
        }
        
        MatchScore = totalConditions > 0 ? matchScore / totalConditions : 0;
        return MatchScore >= Content.MinMatchScore;
    }
    
    private bool EvaluateCondition(AutoFillCondition condition, OrderContextSnapshot snapshot)
    {
        return condition.Field switch
        {
            "OriginalAmount" => CompareValue(snapshot.OriginalAmount, condition.Operator, decimal.Parse(condition.Value)),
            "ItemCount" => CompareValue(snapshot.ItemCount, condition.Operator, int.Parse(condition.Value)),
            "MemberLevel" => CompareString(snapshot.Member?.Level ?? "", condition.Operator, condition.Value),
            "StoreId" => CompareString(snapshot.ExtendedData.GetValueOrDefault("StoreId", ""), condition.Operator, condition.Value),
            "ChannelId" => CompareString(snapshot.ExtendedData.GetValueOrDefault("ChannelId", ""), condition.Operator, condition.Value),
            _ => false
        };
    }
    
    private bool CompareValue<T>(T actual, string operatorStr, T expected) where T : IComparable
    {
        return operatorStr switch
        {
            "=" => actual.CompareTo(expected) == 0,
            "!=" => actual.CompareTo(expected) != 0,
            ">" => actual.CompareTo(expected) > 0,
            "<" => actual.CompareTo(expected) < 0,
            ">=" => actual.CompareTo(expected) >= 0,
            "<=" => actual.CompareTo(expected) <= 0,
            "in" => expected.ToString()?.Split(',').Contains(actual.ToString()) ?? false,
            _ => false
        };
    }
    
    private bool CompareString(string actual, string operatorStr, string expected)
    {
        return operatorStr switch
        {
            "=" => actual == expected,
            "!=" => actual != expected,
            "in" => expected.Split(',').Contains(actual),
            "contains" => actual.Contains(expected),
            _ => false
        };
    }
}

public enum AutoFillTrigger
{
    SimilarOrder,
    SameMemberLevel,
    SameStore,
    SameChannel,
    SameAmountRange,
    ManualSelection
}

public class AutoFillCondition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public decimal Weight { get; set; } = 1m;
    public bool IsRequired { get; set; }
}

public class AutoFillContent
{
    public decimal MinMatchScore { get; set; } = 0.7m;
    
    public string SummaryTemplate { get; set; } = string.Empty;
    public string CustomerExplanationTemplate { get; set; } = string.Empty;
    public string InternalExplanationTemplate { get; set; } = string.Empty;
    
    public List<string> DefaultKeyPoints { get; set; } = new();
    public List<string> DefaultNotes { get; set; } = new();
    
    public Dictionary<string, string> PlaceholderMappings { get; set; } = new();
    
    public string GenerateExplanation(OrderContextSnapshot snapshot, Dictionary<string, object> additionalData)
    {
        var explanation = SummaryTemplate;
        
        foreach (var mapping in PlaceholderMappings)
        {
            var value = GetPlaceholderValue(mapping.Value, snapshot, additionalData);
            explanation = explanation.Replace($"{{{{{mapping.Key}}}}}", value);
        }
        
        return explanation;
    }
    
    private string GetPlaceholderValue(string field, OrderContextSnapshot snapshot, Dictionary<string, object> additionalData)
    {
        if (additionalData.TryGetValue(field, out var value))
            return value?.ToString() ?? "";
        
        return field switch
        {
            "OriginalAmount" => snapshot.OriginalAmount.ToString("F2"),
            "ItemCount" => snapshot.ItemCount.ToString(),
            "MemberLevel" => snapshot.Member?.Level ?? "Normal",
            "StoreId" => snapshot.ExtendedData.GetValueOrDefault("StoreId", ""),
            "ChannelId" => snapshot.ExtendedData.GetValueOrDefault("ChannelId", ""),
            "OrderId" => snapshot.OrderId,
            _ => ""
        };
    }
}

public class TemplateCollaborationService
{
    private readonly Dictionary<string, TemplateCollaboration> _collaborations = new();
    private readonly Dictionary<string, List<RoleAnnotation>> _annotations = new();
    private readonly Dictionary<string, List<ProcessingConclusion>> _conclusions = new();
    private readonly Dictionary<string, TemplateAutoFill> _autoFills = new();
    
    public TemplateCollaboration CreateCollaboration(string templateId, string orderId, SharedExplanation sharedExplanation)
    {
        var collaboration = new TemplateCollaboration
        {
            CollaborationId = Guid.NewGuid().ToString(),
            TemplateId = templateId,
            OrderId = orderId,
            CreatedAt = DateTime.UtcNow,
            SharedExplanation = sharedExplanation,
            Status = CollaborationStatus.Draft
        };
        
        _collaborations[collaboration.CollaborationId] = collaboration;
        return collaboration;
    }
    
    public TemplateCollaboration? GetCollaboration(string collaborationId)
    {
        return _collaborations.TryGetValue(collaborationId, out var c) ? c : null;
    }
    
    public List<TemplateCollaboration> GetCollaborationsByTemplate(string templateId)
    {
        return _collaborations.Values.Where(c => c.TemplateId == templateId).ToList();
    }
    
    public List<TemplateCollaboration> GetCollaborationsByOrder(string orderId)
    {
        return _collaborations.Values.Where(c => c.OrderId == orderId).ToList();
    }
    
    public RoleAnnotation AddAnnotation(
        string collaborationId,
        CollaborationRole role,
        string author,
        string content,
        AnnotationType type = AnnotationType.Note,
        AnnotationPriority priority = AnnotationPriority.Normal,
        string internalNote = "")
    {
        var collaboration = GetCollaboration(collaborationId);
        if (collaboration == null)
            throw new ArgumentException($"Collaboration not found: {collaborationId}");
        
        var annotation = new RoleAnnotation
        {
            AnnotationId = Guid.NewGuid().ToString(),
            TemplateId = collaboration.TemplateId,
            OrderId = collaboration.OrderId,
            Role = role,
            Author = author,
            Content = content,
            InternalNote = internalNote,
            Type = type,
            Priority = priority,
            CreatedAt = DateTime.UtcNow,
            IsVisibleToCustomer = role == CollaborationRole.CustomerService && type == AnnotationType.Note
        };
        
        collaboration.Annotations.Add(annotation);
        collaboration.LastUpdatedAt = DateTime.UtcNow;
        
        collaboration.History.Add(new CollaborationHistory
        {
            Timestamp = DateTime.UtcNow,
            Actor = author,
            ActorRole = role.ToString(),
            Action = "添加备注",
            Details = content
        });
        
        return annotation;
    }
    
    public RoleAnnotation? UpdateAnnotation(string annotationId, string newContent, string updatedBy)
    {
        foreach (var collaboration in _collaborations.Values)
        {
            var annotation = collaboration.Annotations.FirstOrDefault(a => a.AnnotationId == annotationId);
            if (annotation != null)
            {
                annotation.Content = newContent;
                annotation.UpdatedAt = DateTime.UtcNow;
                
                collaboration.History.Add(new CollaborationHistory
                {
                    Timestamp = DateTime.UtcNow,
                    Actor = updatedBy,
                    ActorRole = annotation.Role.ToString(),
                    Action = "更新备注",
                    PreviousValue = annotation.Content,
                    NewValue = newContent
                });
                
                return annotation;
            }
        }
        return null;
    }
    
    public ProcessingConclusion AddConclusion(
        string collaborationId,
        CollaborationRole role,
        string concludedBy,
        ConclusionType type,
        string content,
        List<string> evidence = null)
    {
        var collaboration = GetCollaboration(collaborationId);
        if (collaboration == null)
            throw new ArgumentException($"Collaboration not found: {collaborationId}");
        
        var conclusion = new ProcessingConclusion
        {
            ConclusionId = Guid.NewGuid().ToString(),
            TemplateId = collaboration.TemplateId,
            OrderId = collaboration.OrderId,
            Role = role,
            ConcludedBy = concludedBy,
            ConclusionType = type,
            Content = content,
            ConcludedAt = DateTime.UtcNow,
            SupportingEvidence = evidence ?? new List<string>(),
            IsFinal = type == ConclusionType.Confirmed || type == ConclusionType.Closed
        };
        
        collaboration.Conclusions.Add(conclusion);
        collaboration.LastUpdatedAt = DateTime.UtcNow;
        
        if (conclusion.IsFinal)
        {
            collaboration.Status = CollaborationStatus.Completed;
        }
        else if (type == ConclusionType.Escalated)
        {
            collaboration.Status = CollaborationStatus.Escalated;
        }
        
        collaboration.History.Add(new CollaborationHistory
        {
            Timestamp = DateTime.UtcNow,
            Actor = concludedBy,
            ActorRole = role.ToString(),
            Action = "添加结论",
            Details = $"{type}: {content}"
        });
        
        return conclusion;
    }
    
    public TemplateAutoFill CreateAutoFill(string templateId, AutoFillTrigger trigger, List<AutoFillCondition> conditions, AutoFillContent content)
    {
        var autoFill = new TemplateAutoFill
        {
            AutoFillId = Guid.NewGuid().ToString(),
            TemplateId = templateId,
            Trigger = trigger,
            Conditions = conditions,
            Content = content,
            IsActive = true
        };
        
        _autoFills[autoFill.AutoFillId] = autoFill;
        return autoFill;
    }
    
    public List<TemplateAutoFill> FindMatchingAutoFills(OrderContextSnapshot snapshot)
    {
        return _autoFills.Values
            .Where(af => af.IsActive && af.EvaluateMatch(snapshot))
            .OrderByDescending(af => af.MatchScore)
            .ToList();
    }
    
    public string AutoGenerateExplanation(OrderContextSnapshot snapshot)
    {
        var matchingAutoFills = FindMatchingAutoFills(snapshot);
        
        if (!matchingAutoFills.Any())
            return "";
        
        var bestMatch = matchingAutoFills.First();
        bestMatch.MatchCount++;
        bestMatch.LastMatchedAt = DateTime.UtcNow;
        
        return bestMatch.Content.GenerateExplanation(snapshot, new Dictionary<string, object>());
    }
    
    public TemplateCollaboration UpdateStatus(string collaborationId, CollaborationStatus newStatus, string updatedBy)
    {
        var collaboration = GetCollaboration(collaborationId);
        if (collaboration == null)
            throw new ArgumentException($"Collaboration not found: {collaborationId}");
        
        var oldStatus = collaboration.Status;
        collaboration.Status = newStatus;
        collaboration.LastUpdatedAt = DateTime.UtcNow;
        
        collaboration.History.Add(new CollaborationHistory
        {
            Timestamp = DateTime.UtcNow,
            Actor = updatedBy,
            Action = "状态变更",
            PreviousValue = oldStatus.ToString(),
            NewValue = newStatus.ToString()
        });
        
        return collaboration;
    }
    
    public void LinkRelatedTemplate(string collaborationId, string relatedTemplateId)
    {
        var collaboration = GetCollaboration(collaborationId);
        if (collaboration == null)
            return;
        
        if (!collaboration.RelatedTemplateIds.Contains(relatedTemplateId))
        {
            collaboration.RelatedTemplateIds.Add(relatedTemplateId);
            collaboration.LastUpdatedAt = DateTime.UtcNow;
        }
    }
    
    public string GenerateFullReport(string collaborationId)
    {
        var collaboration = GetCollaboration(collaborationId);
        if (collaboration == null)
            return "协作记录不存在";
        
        return collaboration.GenerateCollaborationSummary();
    }
}