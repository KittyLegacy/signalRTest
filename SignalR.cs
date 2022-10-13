public class ChatHub : Hub
    {

        public readonly DataContext _context;

        public ChatHub(DataContext context)
        {
            _context = context;
        }


        //return connectionId to the client when vendor/user login to chat ()
        public async Task<string> GetConnectionId([Required]int clientId, [Required]string client)  
        {

            if (client.Equals("user"))
            {
                var user = await _context.Users.FindAsync(clientId);

                if (user is null)
                {
                    throw new AccessViolationException("User not Found");
                }                

                user.ConnectionId = user?.ConnectionId ?? new List<string>(); 
                user.ConnectionId.Add(Context.ConnectionId);
                user.IsOnline = true;
                _context.Update(user);
            }

            if (client.Equals("vendor"))
            {
                var vendor = await _context.Vendors.FindAsync(clientId);

                if (vendor is null)
                {
                    throw new AccessViolationException("Vendor not Found");
                }
                          
                vendor.ConnectionId = vendor.ConnectionId ?? new List<string>();
                vendor.ConnectionId.Add(Context.ConnectionId);
                vendor.IsOnline = true;
                _context.Update(vendor);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                throw new HubException(e.Message);
            };

            return Context.ConnectionId;
        }


        //automatically update IsOnline when client disconnect from server
        public override async Task OnDisconnectedAsync(Exception exception)
        {

            Vendor vendor = _context.Vendors.FirstOrDefault(x => x.ConnectionId.Contains(Context.ConnectionId));
            User user = _context.Users.FirstOrDefault(x => x.ConnectionId.Contains(Context.ConnectionId));

            if(vendor is not null)
            {
                var connectionIDs = vendor.ConnectionId;

                connectionIDs?.RemoveAll(x => x.Equals(Context.ConnectionId));
                _context.Update(vendor);

                if (connectionIDs is null || !connectionIDs.Any())
                {
                    vendor.IsOnline = false;
                    _context.Update(vendor);
                }                
            }

            if(user is not null)
            {
                var connectionIDs = user.ConnectionId;

                connectionIDs?.RemoveAll(x => x.Equals(Context.ConnectionId));
                _context.Update(user);

                if (connectionIDs is null || !connectionIDs.Any())
                {
                    user.IsOnline = false;
                    _context.Update(user);
                }
            }
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e) {
                throw new HubException(e.Message);
            };
           
            await base.OnDisconnectedAsync(exception);
        }


        //method to push messages from server to client
        public async Task SendMessage(MessageDto data)
        {

            Vendor vendor = await _context.Vendors.FindAsync(data.VendorId);
            User user = await _context.Users.FindAsync(data.UserId);

            if (vendor is null || user is null) // if-Guard Clause
            {
                throw new AccessViolationException("one or both clients are not available");
            }

            //creating a new Message object to store in the DB
            Message message = new()
            {
                Sender = data.Sender,
                TimeStamp = DateTime.Now,
                Text = data.Text,
                ChatId = data.ChatId,
                //IsSent = isOnline     //assuming if the user/vendor is online, the msg should be delivered            
            };

            //saving message to DB
            _context.Add(message);
            try
            {
                await _context.SaveChangesAsync();
                
            }
            catch (Exception e)
            {
                throw new HubException(e.Message);
            }
        }
    }
        