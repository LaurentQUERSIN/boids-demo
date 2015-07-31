var LeaderboardAPI = {
	get: function(xToken, id, skip, take) {
		var queryString = '?';
		if (skip) queryString += "skip="+skip+'&';
		if (take) queryString += "take="+take+'&';
		//return $.get("https://api.stormancer.com/"+accountId+"/"+applicationName+"/_admin/leaderboard/"+(id?id:'')+queryString, {
		return $.get("https://api.stormancer.com/"+accountId+"/"+applicationName+"/_admin/leaderboard/"+id+"/"+skip+"/"+take, {
			contentType: "application/json",
			dataType: "json",
			beforeSend: function(xhr) {
				xhr.setRequestHeader("x-token", xToken);
				xhr.setRequestHeader("x-version", "1.0");
			}
		});
	}
};
